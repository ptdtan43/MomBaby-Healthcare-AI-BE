using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using System.Threading.Tasks;

namespace MomOi.API.Services.Alert
{
    public class CreateAlertRequestDto
    {
        public string? TargetUserId { get; set; }
        public NotificationAlertType Type { get; set; }
        public int Severity { get; set; } = 50;
        public string Message { get; set; } = string.Empty;
        public string[] Channels { get; set; } = new[] { "app" };
    }

    public class UpdateAlertStatusRequestDto
    {
        public NotificationStatus Status { get; set; }
    }

    public interface IAlertService
    {
        Task<ApiResponse<object>> GetUserAlertsAsync(string userId, NotificationStatus? status);
        Task<ApiResponse<object>> CreateAlertManualAsync(string userId, CreateAlertRequestDto request);
        Task<ApiResponse<object>> UpdateAlertStatusAsync(string userId, int id, UpdateAlertStatusRequestDto request);
        Task<ApiResponse<object>> DeleteAlertAsync(string userId, int id);
        Task<ApiResponse<object>> ResolveAlertAsync(string id);
    }
}
