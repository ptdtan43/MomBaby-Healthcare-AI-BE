using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Alert
{
    public class AlertService : IAlertService
    {
        private readonly AppDbContext _context;

        public AlertService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>> GetUserAlertsAsync(string userId, NotificationStatus? status)
        {
            var query = _context.NotificationAlerts.Where(a => a.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var alerts = await query.OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync();

            return ApiResponse<object>.SuccessResult(alerts, "Lấy danh sách cảnh báo thành công.");
        }

        public async Task<ApiResponse<object>> CreateAlertManualAsync(string userId, CreateAlertRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return ApiResponse<object>.FailureResult("Nội dung cảnh báo không được để trống.");
            }

            var alert = new NotificationAlert
            {
                UserId = userId,
                Type = NotificationAlertType.Symptom,
                Severity = AlertSeverity.Medium,
                Message = request.Message,
                Channels = request.Channels,
                Status = NotificationStatus.Pending,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.NotificationAlerts.Add(alert);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(alert, "Tạo cảnh báo thành công.");
        }

        public async Task<ApiResponse<object>> UpdateAlertStatusAsync(string userId, int id, UpdateAlertStatusRequestDto request)
        {
            var alert = await _context.NotificationAlerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (alert == null) return ApiResponse<object>.FailureResult("Không tìm thấy cảnh báo.");

            alert.Status = request.Status;
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(alert, "Cập nhật trạng thái cảnh báo thành công.");
        }

        public async Task<ApiResponse<object>> DeleteAlertAsync(string userId, int id)
        {
            var alert = await _context.NotificationAlerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (alert == null) return ApiResponse<object>.FailureResult("Không tìm thấy cảnh báo.");

            _context.NotificationAlerts.Remove(alert);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(null!, "Xóa cảnh báo thành công.");
        }
    }
}
