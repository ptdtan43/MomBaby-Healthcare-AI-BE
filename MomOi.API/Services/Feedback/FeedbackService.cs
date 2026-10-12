using MomOi.API.DTOs;
using MomOi.API.DTOs.Feedback;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Services.Feedback
{
    /// <summary>
    /// Logic phản hồi người dùng, chuyển từ FeedbackController xuống.
    /// Kiểm tra dữ liệu đầu vào ở đây (không phải ở controller) để nếu sau này có
    /// thêm kênh gửi phản hồi khác — worker, webhook — thì luật vẫn được áp dụng.
    /// </summary>
    public class FeedbackService : IFeedbackService
    {
        private const int MinMessageLength = 8;

        private readonly IUnitOfWork _unitOfWork;

        public FeedbackService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ApiResponse<object> GetOptions() =>
            ApiResponse<object>.SuccessResult(new
            {
                categories = new[]
                {
                    new { value = "Bug", label = "Lỗi chức năng" },
                    new { value = "Payment", label = "Thanh toán / gói" },
                    new { value = "Content", label = "Nội dung chăm sóc" },
                    new { value = "Idea", label = "Ý tưởng mới" }
                }
            });

        public async Task<ApiResponse<object>> CreateAsync(string userId, CreateFeedbackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Trim().Length < MinMessageLength)
                return ApiResponse<object>.FailureResult(
                    "Nội dung phản hồi quá ngắn.", errorCode: "FEEDBACK_TOO_SHORT");

            var alert = new NotificationAlert
            {
                UserId = userId,
                Type = NotificationAlertType.RoutineCheck,
                Severity = AlertSeverity.Warning,
                Status = NotificationStatus.Pending,
                Channels = new[] { "AdminDashboard" },
                Message = $"[FEEDBACK] Category={dto.Category?.Trim() ?? "General"}; Page={dto.Page?.Trim() ?? "Unknown"}; Message={dto.Message.Trim()}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(new { alert.Id }, "Đã gửi phản hồi thành công.");
        }
    }
}
