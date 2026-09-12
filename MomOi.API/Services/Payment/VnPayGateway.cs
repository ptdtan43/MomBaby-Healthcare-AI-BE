using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MomOi.API.Models.Identity;
using MomOi.API.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MomOi.API.Services.Payment
{
    /// <summary>
    /// Dựng URL thanh toán VNPay và kiểm chữ ký trên callback.
    ///
    /// Quy ước ký đã được kiểm chứng trực tiếp với sandbox VNPay: sắp khoá theo alphabet,
    /// URL-encode cả khoá lẫn giá trị bằng WebUtility (dấu cách thành '+'), nối bằng '&amp;',
    /// rồi HMAC-SHA512 ra hex chữ thường. Chuỗi đem ký và chuỗi đặt lên URL phải giống hệt
    /// nhau từng ký tự — lệch một bước encode là VNPay trả về lỗi sai chữ ký.
    /// </summary>
    public class VnPayGateway : IPaymentGateway
    {
        private readonly VnPayOptions _options;

        public VnPayGateway(IOptions<VnPayOptions> options)
        {
            _options = options.Value;
        }

        public string Provider => "VNPay";

        /// <summary>VNPay chỉ dựng URL tại chỗ, không gọi mạng — nên không có gì để chờ.</summary>
        public Task<string> CreatePaymentUrlAsync(PaymentTransaction txn, string clientIp) =>
            Task.FromResult(BuildPaymentUrl(txn, clientIp));

        /// <summary>Việt Nam cố định UTC+7 và không có giờ mùa hè, nên không cần tra bảng múi giờ.</summary>
        private static DateTime VnNow => DateTime.UtcNow.AddHours(7);

        public string BuildPaymentUrl(PaymentTransaction txn, string clientIp)
        {
            if (string.IsNullOrWhiteSpace(_options.TmnCode) || string.IsNullOrWhiteSpace(_options.HashSecret))
                throw new InvalidOperationException("Chưa cấu hình TmnCode hoặc HashSecret của VNPay.");
            if (string.IsNullOrWhiteSpace(_options.ReturnUrl))
                throw new InvalidOperationException("Chưa cấu hình ReturnUrl của VNPay.");

            var now = VnNow;
            var fields = new Dictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _options.TmnCode,
                // VNPay nhận số tiền đã nhân 100 để loại bỏ phần thập phân.
                ["vnp_Amount"] = ((long)(txn.Amount * 100)).ToString(CultureInfo.InvariantCulture),
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = txn.OrderCode,
                ["vnp_OrderInfo"] = RemoveDiacritics($"Thanh toan goi {txn.PlanCode} cho MomOi"),
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
                ["vnp_ReturnUrl"] = _options.ReturnUrl,
            };

            var hashData = BuildCanonicalString(fields);
            var signature = SignatureHelper.HmacSha512(_options.HashSecret, hashData);

            return $"{_options.PaymentUrl}?{hashData}&vnp_SecureHash={signature}";
        }

        /// <summary>
        /// Kiểm chữ ký trên Return URL và IPN. Trả về false nếu thiếu chữ ký hoặc không khớp.
        /// </summary>
        public bool VerifyCallback(IQueryCollection query)
        {
            var received = query["vnp_SecureHash"].ToString();
            if (string.IsNullOrWhiteSpace(received)) return false;

            // Hai trường này do VNPay thêm vào để chở chữ ký, không tham gia vào phép ký.
            var fields = query
                .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.Ordinal)
                          && kv.Key != "vnp_SecureHash"
                          && kv.Key != "vnp_SecureHashType")
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            if (fields.Count == 0) return false;

            var expected = SignatureHelper.HmacSha512(_options.HashSecret, BuildCanonicalString(fields));
            return SignatureHelper.SecureEquals(expected, received);
        }

        private static string BuildCanonicalString(IDictionary<string, string> fields) =>
            string.Join("&", fields
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

        /// <summary>
        /// VNPay từ chối vnp_OrderInfo có dấu tiếng Việt hoặc ký tự đặc biệt.
        /// </summary>
        private static string RemoveDiacritics(string text)
        {
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                // 'đ' và 'Đ' là ký tự độc lập, không tách được thành chữ cái + dấu.
                sb.Append(c switch { 'đ' => 'd', 'Đ' => 'D', _ => c });
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
