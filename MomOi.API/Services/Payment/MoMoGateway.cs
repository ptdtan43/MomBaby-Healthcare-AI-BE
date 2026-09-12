using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MomOi.API.DTOs.Payment;
using MomOi.API.Models.Identity;
using MomOi.API.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MomOi.API.Services.Payment
{
    /// <summary>
    /// Khác VNPay ở chỗ phải POST sang máy chủ MoMo để xin liên kết, thay vì tự dựng URL.
    /// Chuỗi ký của MoMo cố định theo tài liệu, không sắp xếp động — và accessKey có mặt
    /// trong chuỗi ký nhưng không được gửi trong body.
    /// </summary>
    public class MoMoGateway : IPaymentGateway
    {
        private readonly MoMoOptions _options;
        private readonly HttpClient _http;
        private readonly ILogger<MoMoGateway> _logger;

        public MoMoGateway(IOptions<MoMoOptions> options, HttpClient http, ILogger<MoMoGateway> logger)
        {
            _options = options.Value;
            _http = http;
            _logger = logger;
            // MoMo yêu cầu bên tích hợp chờ tối thiểu 30 giây.
            _http.Timeout = TimeSpan.FromSeconds(35);
        }

        public string Provider => "MoMo";

        public async Task<string> CreatePaymentUrlAsync(PaymentTransaction txn, string clientIp)
        {
            if (string.IsNullOrWhiteSpace(_options.PartnerCode) ||
                string.IsNullOrWhiteSpace(_options.AccessKey) ||
                string.IsNullOrWhiteSpace(_options.SecretKey))
                throw new InvalidOperationException("Chưa cấu hình bộ khoá MoMo.");
            if (string.IsNullOrWhiteSpace(_options.IpnUrl) || string.IsNullOrWhiteSpace(_options.RedirectUrl))
                throw new InvalidOperationException("Chưa cấu hình RedirectUrl hoặc IpnUrl của MoMo.");

            // MoMo chỉ nhận 1.000 đến 50.000.000 đồng, và gửi nguyên giá — không nhân 100 như VNPay.
            var amount = (long)txn.Amount;
            if (amount < 1_000 || amount > 50_000_000)
                throw new InvalidOperationException($"MoMo không nhận số tiền {amount} VND.");

            var requestId = Guid.NewGuid().ToString("N");
            var orderInfo = $"Thanh toan goi {txn.PlanCode} cho MomOi";
            const string requestType = "captureWallet";
            const string extraData = "";

            var raw = $"accessKey={_options.AccessKey}" +
                      $"&amount={amount}" +
                      $"&extraData={extraData}" +
                      $"&ipnUrl={_options.IpnUrl}" +
                      $"&orderId={txn.OrderCode}" +
                      $"&orderInfo={orderInfo}" +
                      $"&partnerCode={_options.PartnerCode}" +
                      $"&redirectUrl={_options.RedirectUrl}" +
                      $"&requestId={requestId}" +
                      $"&requestType={requestType}";

            var body = new
            {
                partnerCode = _options.PartnerCode,
                requestId,
                amount,
                orderId = txn.OrderCode,
                orderInfo,
                redirectUrl = _options.RedirectUrl,
                ipnUrl = _options.IpnUrl,
                requestType,
                extraData,
                lang = "vi",
                signature = SignatureHelper.HmacSha256(_options.SecretKey, raw)
            };

            var response = await _http.PostAsJsonAsync(_options.Endpoint, body);
            var result = await response.Content.ReadFromJsonAsync<MoMoCreateResponse>();

            if (result == null)
                throw new InvalidOperationException("MoMo không trả về nội dung hợp lệ.");

            if (result.ResultCode != 0 || string.IsNullOrWhiteSpace(result.PayUrl))
            {
                _logger.LogError("MoMo từ chối tạo đơn {OrderCode}: resultCode={Code}, message={Message}",
                    txn.OrderCode, result.ResultCode, result.Message);
                throw new InvalidOperationException($"MoMo từ chối tạo đơn: {result.Message}");
            }

            return result.PayUrl;
        }

        /// <summary>
        /// Chuỗi ký của IPN khác chuỗi ký lúc tạo đơn — thứ tự trường theo đúng tài liệu MoMo.
        /// </summary>
        public bool VerifyIpn(MoMoIpnDto dto)
        {
            var raw = $"accessKey={_options.AccessKey}" +
                      $"&amount={dto.Amount}" +
                      $"&extraData={dto.ExtraData}" +
                      $"&message={dto.Message}" +
                      $"&orderId={dto.OrderId}" +
                      $"&orderInfo={dto.OrderInfo}" +
                      $"&orderType={dto.OrderType}" +
                      $"&partnerCode={dto.PartnerCode}" +
                      $"&payType={dto.PayType}" +
                      $"&requestId={dto.RequestId}" +
                      $"&responseTime={dto.ResponseTime}" +
                      $"&resultCode={dto.ResultCode}" +
                      $"&transId={dto.TransId}";

            var expected = SignatureHelper.HmacSha256(_options.SecretKey, raw);
            return SignatureHelper.SecureEquals(expected, dto.Signature);
        }

        private class MoMoCreateResponse
        {
            [JsonPropertyName("resultCode")] public int ResultCode { get; set; }
            [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
            [JsonPropertyName("payUrl")] public string? PayUrl { get; set; }
            [JsonPropertyName("deeplink")] public string? Deeplink { get; set; }
            [JsonPropertyName("qrCodeUrl")] public string? QrCodeUrl { get; set; }
        }
    }
}
