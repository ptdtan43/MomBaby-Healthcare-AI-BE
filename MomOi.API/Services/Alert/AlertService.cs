using MomOi.API.DTOs;
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

        public AlertService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
    }
}
