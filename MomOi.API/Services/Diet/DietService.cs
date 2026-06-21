using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Diet
{
    public class DietService : IDietService
    {
        private readonly IGenericRepository<DietPlan> _dietRepo;
        private readonly IGeminiService _geminiService;

        public DietService(IGenericRepository<DietPlan> dietRepo, IGeminiService geminiService)
        {
            _dietRepo = dietRepo;
            _geminiService = geminiService;
        }

        public async Task<ApiResponse<object>> GenerateDietPlanAsync(string userId)
        {
            var weeklyPlan = new[]
            {
                new
                {
                    day = "Monday",
                    meals = new[]
                    {
                        new { mealType = "Breakfast", recipe = "Phở bò (phần vừa)", calories = 350 },
                        new { mealType = "Lunch", recipe = "Cơm gà + rau luộc", calories = 550 },
                        new { mealType = "Dinner", recipe = "Bún thịt nướng", calories = 480 }
                    }
                },
                new
                {
                    day = "Tuesday",
                    meals = new[]
                    {
                        new { mealType = "Breakfast", recipe = "Bánh mì trứng + sữa đậu", calories = 320 },
                        new { mealType = "Lunch", recipe = "Cơm tấm sườn (ít mỡ)", calories = 580 },
                        new { mealType = "Dinner", recipe = "Canh chua cá + cơm", calories = 450 }
                    }
                },
                new
                {
                    day = "Wednesday",
                    meals = new[]
                    {
                        new { mealType = "Breakfast", recipe = "Xôi gà", calories = 300 },
                        new { mealType = "Lunch", recipe = "Mì Quảng", calories = 520 },
                        new { mealType = "Dinner", recipe = "Đậu hũ sốt cà + rau + cơm", calories = 420 }
                    }
                }
            };

            var dietPlan = new DietPlan
            {
                UserId = userId,
                DailyMealsJson = JsonSerializer.Serialize(weeklyPlan),
                WeekNumber = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dietRepo.AddAsync(dietPlan);
            await _dietRepo.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(dietPlan, "Tạo thực đơn thành công.");
        }

        public async Task<ApiResponse<object>> GetDietPlanAsync(string userId)
        {
            var allPlans = await _dietRepo.FindAsync(d => d.UserId == userId);
            var dietPlan = allPlans.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            if (dietPlan == null) return ApiResponse<object>.FailureResult("Không tìm thấy thực đơn.");

            return ApiResponse<object>.SuccessResult(dietPlan);
        }

        public async Task<ApiResponse<object>> UpdateDietPlanAsync(string userId, UpdateDietPlanRequestDto request)
        {
            var allPlans = await _dietRepo.FindAsync(d => d.UserId == userId);
            var dietPlan = allPlans.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            if (dietPlan == null) return ApiResponse<object>.FailureResult("Không tìm thấy thực đơn để cập nhật.");

            dietPlan.DailyMealsJson = request.DailyMealsJson;
            dietPlan.UpdatedAt = DateTime.UtcNow;

            _dietRepo.Update(dietPlan);
            await _dietRepo.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(dietPlan, "Cập nhật thực đơn thành công.");
        }

        public async Task<ApiResponse<object>> GenerateAiDietAsync(string userId, GenerateAiDietRequestDto request)
        {
            var aiResponseJson = await _geminiService.GenerateAiDietRecipeAsync(request.Query);

            var allPlans = await _dietRepo.FindAsync(d => d.UserId == userId);
            var dietPlan = allPlans.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            if (dietPlan == null) return ApiResponse<object>.FailureResult("Không tìm thấy thực đơn để thêm món AI.");

            return ApiResponse<object>.SuccessResult(new { aiMeal = aiResponseJson }, "Sinh món ăn AI thành công.");
        }
    }
}
