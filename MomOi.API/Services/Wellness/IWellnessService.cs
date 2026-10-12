using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Wellness
{
    /// <summary>
    /// Nội dung chăm sóc tổng quát: lịch chăm sóc theo giai đoạn, cẩm nang khẩn cấp,
    /// thư viện âm thanh thư giãn.
    /// </summary>
    public interface IWellnessService
    {
        /// <summary>Lịch chăm sóc cá nhân hoá theo giai đoạn của mẹ. Cần đọc database.</summary>
        Task<ApiResponse<object>> GetCareCalendarAsync(string userId);

        /// <summary>Cẩm nang dấu hiệu khẩn cấp. Nội dung tĩnh.</summary>
        ApiResponse<object> GetEmergencyGuide();

        /// <summary>Thư viện âm thanh thư giãn. Nội dung tĩnh.</summary>
        ApiResponse<object> GetRelaxTracks();
    }
}
