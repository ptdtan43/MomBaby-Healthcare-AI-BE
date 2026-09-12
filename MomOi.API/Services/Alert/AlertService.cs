using Microsoft.AspNetCore.SignalR;
using MomOi.API.DTOs;
using MomOi.API.Hubs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Alert
{
    public class AlertService : IAlertService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<AlertHub> _hubContext;

        public AlertService(IUnitOfWork unitOfWork, IHubContext<AlertHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task<ApiResponse<object>> GetUserAlertsAsync(string userId, NotificationStatus? status)
        {
            var all = await _unitOfWork.Repository<NotificationAlert>()
                .FindAsync(a => a.UserId == userId && (!status.HasValue || a.Status == status.Value));

            var alerts = all.OrderByDescending(a => a.CreatedAt).Take(100).ToList();

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
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveAlert", new
                {
                    id = alert.Id,
                    ruleId = alert.Id,
                    title = "Lời khuyên từ Care Staff 🩺",
                    message = alert.Message,
                    suggestion = "Đã nhận lời khuyên y tế",
                    severity = alert.Severity.ToString().ToLower(),
                    timestamp = alert.CreatedAt.ToString("o"),
                    status = 0
                });
            }
            catch { }

            return ApiResponse<object>.SuccessResult(alert, "Tạo cảnh báo thành công.");
        }

        public async Task<ApiResponse<object>> UpdateAlertStatusAsync(string userId, int id, UpdateAlertStatusRequestDto request)
        {
            var alert = await _unitOfWork.Repository<NotificationAlert>()
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (alert == null) return ApiResponse<object>.FailureResult("Không tìm thấy cảnh báo.");

            alert.Status = request.Status;
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(alert, "Cập nhật trạng thái cảnh báo thành công.");
        }

        public async Task<ApiResponse<object>> DeleteAlertAsync(string userId, int id)
        {
            var alert = await _unitOfWork.Repository<NotificationAlert>()
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (alert == null) return ApiResponse<object>.FailureResult("Không tìm thấy cảnh báo.");

            _unitOfWork.Repository<NotificationAlert>().Remove(alert);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(null!, "Xóa cảnh báo thành công.");
        }

        public async Task<ApiResponse<object>> ResolveAlertAsync(string id)
        {
            // 1. Resolve CriticalAlertLogs
            var criticals = await _unitOfWork.Repository<CriticalAlertLog>()
                .FindAsync(c => !c.IsResolved && (c.UserId == id || c.Id.ToString() == id));
            foreach (var c in criticals)
            {
                c.IsResolved = true;
            }

            // 2. Resolve LifestyleAlerts
            var lifestyleAlerts = await _unitOfWork.Repository<LifestyleAlert>()
                .FindAsync(l => l.Status == AlertStatus.Pending && (l.UserId == id || l.Id.ToString() == id));
            foreach (var l in lifestyleAlerts)
            {
                l.Status = AlertStatus.Resolved;
            }

            // 3. Resolve NotificationAlerts
            var notificationAlerts = await _unitOfWork.Repository<NotificationAlert>()
                .FindAsync(n => n.Status == NotificationStatus.Pending && (n.UserId == id || n.Id.ToString() == id));
            foreach (var n in notificationAlerts)
            {
                n.Status = NotificationStatus.Resolved;
            }

            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<object>.SuccessResult(null!, "Đã giải quyết cảnh báo thành công.");
        }
    }
}
