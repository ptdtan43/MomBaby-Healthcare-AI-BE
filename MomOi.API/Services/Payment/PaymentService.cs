using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MomOi.API.Constants;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Payment;
using MomOi.API.Models.Identity;
using MomOi.API.Options;
using MomOi.API.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MomOi.API.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEnumerable<IPaymentGateway> _gateways;
        private readonly VnPayGateway _vnPay;
        private readonly MoMoGateway _moMo;
        private readonly BankTransferOptions _bankTransferOptions;
        private readonly SePayBankTransferVerifier _sePayVerifier;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IEnumerable<IPaymentGateway> gateways,
            VnPayGateway vnPay,
            MoMoGateway moMo,
            IOptions<BankTransferOptions> bankTransferOptions,
            SePayBankTransferVerifier sePayVerifier,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _gateways = gateways;
            _vnPay = vnPay;
            _moMo = moMo;
            _bankTransferOptions = bankTransferOptions.Value;
            _sePayVerifier = sePayVerifier;
            _logger = logger;
        }

        public IReadOnlyCollection<PlanDto> GetPlans() =>
            SubscriptionPlans.List()
                .Select(p => new PlanDto
                {
                    Code = p.Code,
                    Name = p.Name,
                    Price = p.Price,
                    Months = p.Months,
                    Tier = p.Tier.ToString()
                })
                .ToList();

        public async Task<ApiResponse<CreatePaymentResponseDto>> CreatePaymentAsync(
            string userId, CreatePaymentRequestDto dto, string clientIp)
        {
            var plan = SubscriptionPlans.Get(dto.PlanCode);
            if (plan == null)
                return ApiResponse<CreatePaymentResponseDto>.FailureResult(
                    "Goi thue bao khong ton tai.", errorCode: "PLAN_NOT_FOUND");

            var isBankTransfer = string.Equals(dto.Provider, "BankTransfer", StringComparison.OrdinalIgnoreCase);
            var gateway = isBankTransfer
                ? null
                : _gateways.FirstOrDefault(g =>
                    string.Equals(g.Provider, dto.Provider, StringComparison.OrdinalIgnoreCase));

            if (!isBankTransfer && gateway == null)
                return ApiResponse<CreatePaymentResponseDto>.FailureResult(
                    $"Khong ho tro cong thanh toan '{dto.Provider}'.", errorCode: "PROVIDER_UNSUPPORTED");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<CreatePaymentResponseDto>.FailureResult("Khong tim thay nguoi dung.");

            var txn = new PaymentTransaction
            {
                UserId = userId,
                OrderCode = await GenerateUniqueOrderCodeAsync(),
                PlanCode = plan.Code,
                TargetTier = plan.Tier,
                DurationMonths = plan.Months,
                Amount = plan.Price,
                Currency = "VND",
                PaymentMethod = isBankTransfer ? "BankTransfer" : gateway!.Provider,
                Status = PaymentStatus.Pending
            };

            var transferMemo = BuildTransferMemo(txn.OrderCode);
            string payUrl;
            if (isBankTransfer)
            {
                payUrl = BuildVietQrUrl(txn.Amount, transferMemo);
            }
            else
            {
                try
                {
                    payUrl = await gateway!.CreatePaymentUrlAsync(txn, clientIp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot create payment URL for {Provider}, plan {PlanCode}.",
                        gateway!.Provider, plan.Code);
                    return ApiResponse<CreatePaymentResponseDto>.FailureResult(
                        "Khong ket noi duoc cong thanh toan. Vui long thu lai.",
                        errorCode: "GATEWAY_ERROR");
                }
            }

            await _unitOfWork.Repository<PaymentTransaction>().AddAsync(txn);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created payment {OrderCode} via {Provider} for user {UserId}, plan {PlanCode}, {Amount} VND.",
                txn.OrderCode, txn.PaymentMethod, userId, plan.Code, plan.Price);

            return ApiResponse<CreatePaymentResponseDto>.SuccessResult(new CreatePaymentResponseDto
            {
                OrderCode = txn.OrderCode,
                PayUrl = payUrl,
                QrUrl = isBankTransfer ? payUrl : string.Empty,
                Amount = plan.Price,
                PlanName = plan.Name,
                PlanCode = plan.Code,
                Provider = txn.PaymentMethod,
                TransferMemo = isBankTransfer ? transferMemo : string.Empty,
                BankInfo = isBankTransfer ? BuildBankInfo() : null,
                CreatedAt = txn.CreatedAt
            }, "Da tao lien ket thanh toan.");
        }

        public async Task<IpnResult> HandleVnPayIpnAsync(IQueryCollection query)
        {
            if (!_vnPay.VerifyCallback(query))
            {
                _logger.LogWarning("Rejected VNPay IPN: invalid signature. TxnRef={TxnRef}",
                    query["vnp_TxnRef"].ToString());
                return new IpnResult("97", "Invalid signature");
            }

            if (!long.TryParse(query["vnp_Amount"].ToString(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var rawAmount))
                return new IpnResult("04", "Invalid amount");

            var succeeded = query["vnp_ResponseCode"].ToString() == "00"
                         && query["vnp_TransactionStatus"].ToString() == "00";

            return await ConfirmAsync(
                orderCode: query["vnp_TxnRef"].ToString(),
                amountVnd: rawAmount / 100m,
                succeeded: succeeded,
                failureReason: succeeded ? null
                    : $"vnp_ResponseCode={query["vnp_ResponseCode"]}, vnp_TransactionStatus={query["vnp_TransactionStatus"]}",
                providerTxnNo: query["vnp_TransactionNo"].ToString(),
                rawPayload: JsonSerializer.Serialize(query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())));
        }

        public async Task<IpnResult> HandleMoMoIpnAsync(MoMoIpnDto dto)
        {
            if (!_moMo.VerifyIpn(dto))
            {
                _logger.LogWarning("Rejected MoMo IPN: invalid signature. orderId={OrderId}", dto.OrderId);
                return new IpnResult("97", "Invalid signature");
            }

            return await ConfirmAsync(
                orderCode: dto.OrderId,
                amountVnd: dto.Amount,
                succeeded: dto.ResultCode == 0,
                failureReason: dto.ResultCode == 0 ? null : $"resultCode={dto.ResultCode}, message={dto.Message}",
                providerTxnNo: dto.TransId.ToString(CultureInfo.InvariantCulture),
                rawPayload: JsonSerializer.Serialize(dto));
        }

        private async Task<IpnResult> ConfirmAsync(
            string orderCode,
            decimal amountVnd,
            bool succeeded,
            string? failureReason,
            string providerTxnNo,
            string rawPayload)
        {
            var repo = _unitOfWork.Repository<PaymentTransaction>();
            var txn = await repo.FirstOrDefaultAsync(t => t.OrderCode == orderCode);

            if (txn == null)
            {
                _logger.LogWarning("Payment order not found: {OrderCode}", orderCode);
                return new IpnResult("01", "Order not found");
            }

            if (amountVnd != txn.Amount)
            {
                _logger.LogWarning("Invalid amount for {OrderCode}: gateway {Gateway}, stored {Stored}.",
                    orderCode, amountVnd, txn.Amount);
                return new IpnResult("04", "Invalid amount");
            }

            if (txn.Status != PaymentStatus.Pending)
                return new IpnResult("02", "Order already confirmed");

            txn.RawCallback = rawPayload;
            txn.ProviderTxnNo = providerTxnNo;
            txn.TransactionId = providerTxnNo;
            txn.UpdatedAt = DateTime.UtcNow;

            if (!succeeded)
            {
                txn.Status = PaymentStatus.Failed;
                txn.FailureReason = failureReason;
                repo.Update(txn);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Payment {OrderCode} failed: {Reason}", orderCode, failureReason);
                return new IpnResult("00", "Confirm Success");
            }

            var user = await _userManager.FindByIdAsync(txn.UserId);
            if (user == null)
            {
                txn.Status = PaymentStatus.Failed;
                txn.FailureReason = "Payment user not found.";
                repo.Update(txn);
                await _unitOfWork.SaveChangesAsync();
                return new IpnResult("01", "Order not found");
            }

            var startFrom = user.TierExpiresAt is { } expiry && expiry > DateTime.UtcNow
                ? expiry
                : DateTime.UtcNow;

            user.Tier = txn.TargetTier;
            user.TierExpiresAt = startFrom.AddMonths(txn.DurationMonths);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Cannot update tier for user {UserId}: {Errors}",
                    user.Id, string.Join("; ", updateResult.Errors.Select(e => e.Description)));
                return new IpnResult("99", "Failed to update account");
            }

            txn.Status = PaymentStatus.Completed;
            txn.PaidAt = DateTime.UtcNow;
            repo.Update(txn);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Payment {OrderCode} completed. User {UserId} tier {Tier} until {Expiry}.",
                orderCode, user.Id, user.Tier, user.TierExpiresAt);

            return new IpnResult("00", "Confirm Success");
        }

        public async Task<ApiResponse<PaymentStatusDto>> GetStatusAsync(string userId, string orderCode)
        {
            var repo = _unitOfWork.Repository<PaymentTransaction>();
            var txn = await repo.FirstOrDefaultAsync(t => t.OrderCode == orderCode && t.UserId == userId);

            if (txn == null)
                return ApiResponse<PaymentStatusDto>.FailureResult("Khong tim thay giao dich.");

            if (txn.Status == PaymentStatus.Pending
                && string.Equals(txn.PaymentMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase))
            {
                var matched = await _sePayVerifier.FindMatchAsync(txn, BuildTransferMemo(txn.OrderCode));
                if (matched != null)
                {
                    var alreadyUsed = await repo.ExistsAsync(t =>
                        t.ProviderTxnNo == matched.ProviderTxnNo
                        && t.Status == PaymentStatus.Completed
                        && t.OrderCode != txn.OrderCode);

                    if (!alreadyUsed)
                    {
                        await ConfirmAsync(
                            orderCode: txn.OrderCode,
                            amountVnd: matched.AmountIn,
                            succeeded: true,
                            failureReason: null,
                            providerTxnNo: matched.ProviderTxnNo,
                            rawPayload: matched.RawPayload);

                        txn = await repo.FirstOrDefaultAsync(t => t.OrderCode == orderCode && t.UserId == userId) ?? txn;
                    }
                    else
                    {
                        _logger.LogWarning("SePay transaction {ProviderTxnNo} was already used.", matched.ProviderTxnNo);
                    }
                }
            }

            var user = await _userManager.FindByIdAsync(userId);

            return ApiResponse<PaymentStatusDto>.SuccessResult(new PaymentStatusDto
            {
                OrderCode = txn.OrderCode,
                PlanCode = txn.PlanCode,
                Amount = txn.Amount,
                Status = txn.Status,
                FailureReason = txn.FailureReason,
                PaidAt = txn.PaidAt,
                CurrentTier = user?.EffectiveTier ?? SubscriptionTier.Free,
                TierExpiresAt = user?.TierExpiresAt
            });
        }

        public async Task<ApiResponse<object>> GetHistoryAsync(string userId)
        {
            var transactions = await _unitOfWork.Repository<PaymentTransaction>()
                .FindAsync(t => t.UserId == userId);

            var user = await _userManager.FindByIdAsync(userId);
            var result = transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.OrderCode,
                    t.PlanCode,
                    tier = t.TargetTier.ToString(),
                    t.DurationMonths,
                    t.Amount,
                    t.Currency,
                    t.PaymentMethod,
                    status = t.Status.ToString(),
                    t.ProviderTxnNo,
                    t.FailureReason,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.PaidAt
                })
                .ToList();

            return ApiResponse<object>.SuccessResult(new
            {
                currentTier = user?.EffectiveTier.ToString() ?? SubscriptionTier.Free.ToString(),
                tierExpiresAt = user?.TierExpiresAt,
                transactions = result
            }, "Lay lich su thanh toan thanh cong.");
        }

        private static string GenerateOrderCode() =>
            $"MOMOI{DateTime.UtcNow.AddHours(7):yyyyMMddHHmmss}{Random.Shared.Next(1000, 10000)}";

        private async Task<string> GenerateUniqueOrderCodeAsync()
        {
            var repo = _unitOfWork.Repository<PaymentTransaction>();
            for (var i = 0; i < 5; i++)
            {
                var orderCode = GenerateOrderCode();
                if (!await repo.ExistsAsync(t => t.OrderCode == orderCode))
                    return orderCode;
            }

            return $"{GenerateOrderCode()}{Guid.NewGuid():N}"[..32].ToUpperInvariant();
        }

        private static string BuildTransferMemo(string orderCode) => orderCode;

        private string BuildVietQrUrl(decimal amount, string memo)
        {
            var sanitizedMemo = Regex.Replace(memo, "[^a-zA-Z0-9 ]", string.Empty).Trim();
            var amountText = decimal.Truncate(amount).ToString("0", CultureInfo.InvariantCulture);
            var accountName = Uri.EscapeDataString(_bankTransferOptions.AccountName);
            var addInfo = Uri.EscapeDataString(sanitizedMemo);

            var bankIdentifier = string.IsNullOrWhiteSpace(_bankTransferOptions.Bin)
                ? _bankTransferOptions.BankId
                : _bankTransferOptions.Bin;

            return $"https://img.vietqr.io/image/{bankIdentifier}-{_bankTransferOptions.AccountNumber}-{_bankTransferOptions.VietQrTemplate}.png?amount={amountText}&addInfo={addInfo}&accountName={accountName}";
        }

        private BankTransferInfoDto BuildBankInfo() => new()
        {
            BankId = _bankTransferOptions.BankId,
            Bin = _bankTransferOptions.Bin,
            BankName = _bankTransferOptions.BankName,
            BankShortName = _bankTransferOptions.BankShortName,
            AccountNumber = _bankTransferOptions.AccountNumber,
            AccountName = _bankTransferOptions.AccountName,
            AccountHolderDisplay = string.IsNullOrWhiteSpace(_bankTransferOptions.AccountHolderDisplay)
                ? _bankTransferOptions.AccountName
                : _bankTransferOptions.AccountHolderDisplay
        };
    }
}
