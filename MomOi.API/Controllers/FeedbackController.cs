using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Feedback;
using MomOi.API.Models.Health;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/feedback")]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("options")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GetOptions()
        {
            return Ok(ApiResponse<object>.SuccessResult(new
            {
                categories = new[]
                {
                    new { value = "Bug", label = "Lỗi chức năng" },
                    new { value = "Payment", label = "Thanh toán / gói" },
                    new { value = "Content", label = "Nội dung chăm sóc" },
                    new { value = "Idea", label = "Ý tưởng mới" }
                }
            }));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse<object>.FailureResult("Vui long dang nhap lai."));

            if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Trim().Length < 8)
                return BadRequest(ApiResponse<object>.FailureResult("Noi dung phan hoi qua ngan."));

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

            _context.NotificationAlerts.Add(alert);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { alert.Id }, "Da gui phan hoi thanh cong."));
        }
    }
}
