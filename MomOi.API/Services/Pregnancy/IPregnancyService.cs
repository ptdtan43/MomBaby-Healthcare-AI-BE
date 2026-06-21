using MomOi.API.DTOs;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Services.Pregnancy
{
    public interface IPregnancyService
    {
        Task<ApiResponse<object>> SetupPregnancyAsync(string userId, DateTime lastMenstrualPeriod, DateTime? dueDate);
        Task<ApiResponse<object>> GetThisWeekAsync(string userId);
        Task<ApiResponse<object>> LogFoodAsync(string userId, string[] foods);
        Task<ApiResponse<object>> GetMealPlanAsync(string userId, int? week);
        Task<ApiResponse<object>> LogWeightAsync(string userId, float weightKg, DateTime date);
        Task<ApiResponse<object>> GetExercisePlanAsync(string userId);
        Task<ApiResponse<object>> LogExerciseAsync(string userId, int stepCount, string exerciseType, int durationMinutes);
    }
}
