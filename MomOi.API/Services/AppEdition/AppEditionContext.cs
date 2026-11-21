using System;
using Microsoft.AspNetCore.Http;

namespace MomOi.API.Services.AppEdition
{
    /// <summary>
    /// Cho biết request đến từ bản app nào.
    ///
    /// Bản Android trên Google Play phát hành bằng tài khoản cá nhân, nên Google không cho có nội dung
    /// mang thai, thụ thai hay sức khỏe sinh sản (email chuyên viên chính sách Google Play, 20/11/2026).
    /// App bản Play gửi header "X-App-Edition: play". Khi có header này, server bỏ các nội dung đó
    /// (lời khuyên thụ thai, thông báo BR01, prompt AI theo giai đoạn chuẩn bị mang thai).
    /// Web và các bản khác không gửi header nên giữ nguyên hành vi cũ.
    /// </summary>
    public interface IAppEditionContext
    {
        bool IsPlayEdition { get; }
    }

    public class AppEditionContext : IAppEditionContext
    {
        public const string HeaderName = "X-App-Edition";
        public const string PlayEdition = "play";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppEditionContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsPlayEdition
        {
            get
            {
                var headers = _httpContextAccessor.HttpContext?.Request.Headers;
                if (headers == null || !headers.TryGetValue(HeaderName, out var value)) return false;
                return string.Equals(value.ToString().Trim(), PlayEdition, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
