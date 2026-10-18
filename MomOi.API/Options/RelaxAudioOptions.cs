using System.Collections.Generic;

namespace MomOi.API.Options
{
    /// <summary>
    /// Nguồn file nhạc cho thư viện thư giãn (GET /api/wellness/relax-tracks).
    /// Mỗi bài lấy link theo thứ tự: <see cref="Tracks"/> (link ngoài, vd CDN) →
    /// file <c>wwwroot/audio/{id}.mp3</c> do API tự phục vụ → không có (client tự tạo âm thanh).
    /// </summary>
    public class RelaxAudioOptions
    {
        public const string SectionName = "RelaxAudio";

        /// <summary>
        /// Địa chỉ công khai của API, vd "https://momoi-api-production.up.railway.app".
        /// Cần đặt khi chạy sau proxy (Railway): Request.Scheme ở đó là http, link http
        /// sẽ bị trình duyệt chặn trên trang web https. Để trống thì dùng địa chỉ của request.
        /// </summary>
        public string PublicBaseUrl { get; set; } = string.Empty;

        /// <summary>Link tuyệt đối theo id bài, ưu tiên hơn file trong wwwroot/audio.</summary>
        public Dictionary<string, string> Tracks { get; set; } = new();
    }
}
