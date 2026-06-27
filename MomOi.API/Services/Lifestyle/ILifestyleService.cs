using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Lifestyle
{
    public class LifestyleEntryRequestDto
    {
        public float SelfCareHours { get; set; }
        public float SleepHours { get; set; }
        public float PhysicalHours { get; set; }
        public float SocialHours { get; set; }
        public float WaterLiters { get; set; }
        public string StressLevel { get; set; } = "Low";
    }

    public interface ILifestyleService
    {
        Task<ApiResponse<object>> SubmitLifestyleEntryAsync(string userId, LifestyleEntryRequestDto request);
        Task<ApiResponse<object>> GetTodayEntryAsync(string userId);
        Task<ApiResponse<object>> GetHistoryAsync(string userId, int days);
        Task<ApiResponse<object>> GetAlertsAsync(string userId);
        Task<ApiResponse<object>> GetSummaryAsync(string userId);
    }
}
