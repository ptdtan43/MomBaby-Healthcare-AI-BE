using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MomOi.API.Constants;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Payment;
using MomOi.API.Models.Identity;
using MomOi.API.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
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
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IEnumerable<IPaymentGateway> gateways,
            VnPayGateway vnPay,
            MoMoGateway moMo,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _gateways = gateways;
            _vnPay = vnPay;
            _moMo = moMo;
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

        // ─── Tạo giao dịch ──────────────────────────────────────────────────────

        public async Task<ApiResponse<CreatePaymentResponseDto>> CreatePaymentAsync(
            string userId, CreatePaymentRequestDto dto, string clientIp)
        {
            var plan = SubscriptionPlans.Get(dto.PlanCode);
            if (plan == null)
                return ApiResponse<CreatePaymentResponseDto>.FailureResult(
                    "Gói thuê bao không tồn tại.", errorCode: "PLAN_NOT_FOUND");

            var gateway = _gateways.FirstOrDefault(g =>
                string.Equals(g.Provider, dto.Provider, StringComparison.OrdinalIgnoreCase));
            if (gateway == null)
                return ApiResponse<CreatePaymentResponseDto>.FailureResult(
                    $"Không hỗ trợ cổng thanh toán '{dto.Provider}'.", errorCode: "PROVIDER_UNSUPPORTED");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<CreatePaymentResponseDto>.FailureResult("Không tìm thấy người dùng.");

            var txn = new PaymentTransaction
            {
                UserId = userId,
                OrderCode = GenerateOrderCode(),
                PlanCode = plan.Code,
                TargetTier = plan.Tier,
                DurationMonths = plan.Months,
                Amount = plan.Price,
                Currency = "VND",
                PaymentMethod = gateway.Provider,
                Status = PaymentStatus.Pending
            };

            string payUrl;
            try
            {
                payUrl = await gateway.CreatePaymentUrlAsync(txn, clientIp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không tạo được liên kết thanh toán {Provider} cho gói {PlanCode}.",
                    gateway.Provider, plan.Code);
                return ApiResponse<CreatePaymentResponseDto>.FailureResult(
                    "Không kết nối được cổng thanh toán. Vui lòng thử lại.",
                    errorCode: "GATEWAY_ERROR");
            }

            await _unitOfWork.Repository<PaymentTransaction>().AddAsync(txn);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Tạo giao dịch {OrderCode} qua {Provider} cho user {UserId}, gói {PlanCode}, {Amount} VND.",
                txn.OrderCode, gateway.Provider, userId, plan.Code, plan.Price);

            return ApiResponse<CreatePaymentResponseDto>.SuccessResult(new CreatePaymentResponseDto
            {
                OrderCode = txn.OrderCode,
                PayUrl = payUrl,
                Amount = plan.Price,
                PlanName = plan.Name,
                Provider = gateway.Provider
            }, "Đã tạo liên kết thanh toán.");
        }

        // ─── IPN VNPay ──────────────────────────────────────────────────────────

        public async Task<IpnResult> HandleVnPayIpnAsync(IQueryCollection query)
        {
            if (!_vnPay.VerifyCallback(query))
            {
                _logger.LogWarning("IPN VNPay bị từ chối: chữ ký không hợp lệ. TxnRef={TxnRef}",
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
                // VNPay gửi số tiền đã nhân 100.
                amountVnd: rawAmount / 100m,
                succeeded: succeeded,
                failureReason: succeeded ? null
                    : $"vnp_ResponseCode={query["vnp_ResponseCode"]}, vnp_TransactionStatus={query["vnp_TransactionStatus"]}",
                providerTxnNo: query["vnp_TransactionNo"].ToString(),
                rawPayload: JsonSerializer.Serialize(query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())));
        }

        // ─── IPN MoMo ───────────────────────────────────────────────────────────

        public async Task<IpnResult> HandleMoMoIpnAsync(MoMoIpnDto dto)
        {
            if (!_moMo.VerifyIpn(dto))
            {
                _logger.LogWarning("IPN MoMo bị từ chối: chữ ký không hợp lệ. orderId={OrderId}", dto.OrderId);
                return new IpnResult("97", "Invalid signature");
            }

            return await ConfirmAsync(
                orderCode: dto.OrderId,
                // MoMo gửi nguyên giá, không nhân 100.
                amountVnd: dto.Amount,
                succeeded: dto.ResultCode == 0,
                failureReason: dto.ResultCode == 0 ? null : $"resultCode={dto.ResultCode}, message={dto.Message}",
                providerTxnNo: dto.TransId.ToString(CultureInfo.InvariantCulture),
                rawPayload: JsonSerializer.Serialize(dto));
        }

        // ─── Phần xác nhận dùng chung cho mọi cổng ──────────────────────────────

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
                _logger.LogWarning("IPN cho đơn không tồn tại: {OrderCode}", orderCode);
                return new IpnResult("01", "Order not found");
            }

            // Số tiền cổng báo phải khớp đơn đã lưu, nếu không là có người can thiệp.
            if (amountVnd != txn.Amount)
            {
                _logger.LogWarning("IPN sai số tiền cho {OrderCode}: cổng báo {Gateway}, đơn lưu {Stored}.",
                    orderCode, amountVnd, txn.Amount);
                return new IpnResult("04", "Invalid amount");
            }

            // Chống trùng: cổng gọi lại nhiều lần cho tới khi nhận được mã xác nhận hợp lệ.
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

                _logger.LogInformation("Giao dịch {OrderCode} thất bại: {Reason}", orderCode, failureReason);
                return new IpnResult("00", "Confirm Success");
            }

            var user = await _userManager.FindByIdAsync(txn.UserId);
            if (user == null)
            {
                txn.Status = PaymentStatus.Failed;
                txn.FailureReason = "Không tìm thấy người dùng của giao dịch.";
                repo.Update(txn);
                await _unitOfWork.SaveChangesAsync();
                return new IpnResult("01", "Order not found");
            }

            // Gia hạn cộng dồn: còn hạn thì nối tiếp, hết hạn thì tính từ hôm nay.
            var startFrom = user.TierExpiresAt is { } expiry && expiry > DateTime.UtcNow
                ? expiry
                : DateTime.UtcNow;

            user.Tier = txn.TargetTier;
            user.TierExpiresAt = startFrom.AddMonths(txn.DurationMonths);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Không cập nhật được tier cho user {UserId}: {Errors}",
                    user.Id, string.Join("; ", updateResult.Errors.Select(e => e.Description)));
                // Giao dịch vẫn Pending nên lần gọi lại sau xử lý tiếp được.
                return new IpnResult("99", "Failed to update account");
            }

            txn.Status = PaymentStatus.Completed;
            txn.PaidAt = DateTime.UtcNow;
            repo.Update(txn);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Giao dịch {OrderCode} thành công. User {UserId} lên {Tier} đến {Expiry}.",
                orderCode, user.Id, user.Tier, user.TierExpiresAt);

            return new IpnResult("00", "Confirm Success");
        }

        // ─── Tra trạng thái ─────────────────────────────────────────────────────

        public async Task<ApiResponse<PaymentStatusDto>> GetStatusAsync(string userId, string orderCode)
        {
            var txn = await _unitOfWork.Repository<PaymentTransaction>()
                .FirstOrDefaultAsync(t => t.OrderCode == orderCode && t.UserId == userId);

            if (txn == null)
                return ApiResponse<PaymentStatusDto>.FailureResult("Không tìm thấy giao dịch.");

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

        /// <summary>
        /// Mã đơn phải duy nhất, chỉ gồm chữ và số. Dấu thời gian bảo đảm không trùng giữa
        /// các giây; bốn số ngẫu nhiên phòng hai giao dịch rơi vào cùng một giây. Với MoMo
        /// điều này càng quan trọng vì bộ khoá thử nghiệm dùng chung partnerCode với nhiều bên.
        /// </summary>
        private static string GenerateOrderCode() =>
            $"MOMOI{DateTime.UtcNow.AddHours(7):yyyyMMddHHmmss}{Random.Shared.Next(1000, 10000)}";
    }
}
