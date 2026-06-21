using MomOi.API.DTOs;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Services.DailyMonitoring
{
    public class DailyMonitoringRequestDto
    {
        public DateTime? Date { get; set; }
        // Sleep
        public float? SleepHours { get; set; }
        public int? SleepQuality { get; set; }
        // Water
        public float? WaterLiters { get; set; }
        // Meals
        public bool? HadBreakfast { get; set; }
        public bool? HadLunch { get; set; }
        public bool? HadDinner { get; set; }
        // Mood
        public int? MoodScore { get; set; }
        public string? MoodNote { get; set; }
        // Vitals
        public float? BloodSugar { get; set; }
        public int? BloodPressureHigh { get; set; }
        public int? BloodPressureLow { get; set; }
        public float? Weight { get; set; }
        // Symptoms
        public int? SymptomSeverity { get; set; }
        public string? SymptomNote { get; set; }
        // Activity & Baby Metrics
        public int? Steps { get; set; }
        public float? BabyIronInput { get; set; }
        public string? BabyFoodTexture { get; set; }
        public int? BabyFishServings { get; set; }
        public int? EpdsScore { get; set; }
        public int? ConceptionDayOfCycle { get; set; }
        public bool? AllergySymptomLogged { get; set; }
        public string? NewFoodLogged { get; set; }
    }

    public interface IDailyMonitoringService
    {
        Task<ApiResponse<object>> CreateOrUpdateDailyMonitoringAsync(string userId, DailyMonitoringRequestDto request);
        Task<ApiResponse<object>> GetTodayMonitoringAsync(string userId);
        Task<ApiResponse<object>> GetHistoryAsync(string userId, int limit);
        Task<ApiResponse<object>> GetInsightsAsync(string userId, int days);
    }
}
