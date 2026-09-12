using System;
using System.Security.Cryptography;
using System.Text;

namespace MomOi.API.Services.Payment
{
    public static class SignatureHelper
    {
        /// <summary>VNPay ký bằng HMAC-SHA512, kết quả hex chữ thường.</summary>
        public static string HmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
        }

        /// <summary>MoMo ký bằng HMAC-SHA256, kết quả hex chữ thường.</summary>
        public static string HmacSha256(string key, string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
        }

        /// <summary>
        /// So sánh hai chữ ký theo thời gian hằng số, tránh rò rỉ thông tin qua thời gian phản hồi.
        /// </summary>
        public static bool SecureEquals(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(a.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(b.ToLowerInvariant()));
        }
    }
}
