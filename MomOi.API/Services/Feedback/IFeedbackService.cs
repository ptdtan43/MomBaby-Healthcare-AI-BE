using MomOi.API.DTOs;
using MomOi.API.DTOs.Feedback;
using System.Threading.Tasks;

namespace MomOi.API.Services.Feedback
{
    public interface IFeedbackService
    {
        /// <summary>Danh mục phân loại phản hồi cho FE dựng dropdown. Nội dung tĩnh.</summary>
        ApiResponse<object> GetOptions();

        /// <summary>Ghi nhận phản hồi của người dùng, đẩy vào hàng chờ của Admin.</summary>
        Task<ApiResponse<object>> CreateAsync(string userId, CreateFeedbackDto dto);
    }
}
