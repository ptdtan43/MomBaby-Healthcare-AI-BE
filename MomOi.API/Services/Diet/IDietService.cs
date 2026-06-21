using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Diet
{
    public class GenerateAiDietRequestDto
    {
        public string Query { get; set; } = string.Empty;
    }

    public class UpdateDietPlanRequestDto
    {
        public string DailyMealsJson { get; set; } = string.Empty;
    }

    public interface IDietService
    {
        Task<ApiResponse<object>> GenerateDietPlanAsync(string userId);
        Task<ApiResponse<object>> GetDietPlanAsync(string userId);
        Task<ApiResponse<object>> UpdateDietPlanAsync(string userId, UpdateDietPlanRequestDto request);
        Task<ApiResponse<object>> GenerateAiDietAsync(string userId, GenerateAiDietRequestDto request);
    }
}
