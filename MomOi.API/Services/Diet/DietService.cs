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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeminiService _geminiService;

        public DietService(IUnitOfWork unitOfWork, IGeminiService geminiService)
        {
            _unitOfWork = unitOfWork;
            _geminiService = geminiService;
        }

        public Task<ApiResponse<object>> GenerateDietPlanAsync(string userId)
        {
            // Trước kia hàm này lưu một "thực đơn tuần" viết cứng (3 ngày, calo đoán) như dữ liệu thật.
            // Thực đơn thai kỳ thật lấy từ nutrition engine (USDA) tại GET /api/pregnancy/meal-plan;
            // không client nào gọi endpoint này nên báo rõ thay vì tạo dữ liệu giả.
            return Task.FromResult(ApiResponse<object>.FailureResult(
                "Thực đơn được tạo theo tuần thai từ dữ liệu dinh dưỡng USDA. Vui lòng dùng mục Thực đơn thai kỳ."));
        }

        public async Task<ApiResponse<object>> GetDietPlanAsync(string userId)
        {
            var allPlans = await _unitOfWork.Repository<DietPlan>().FindAsync(d => d.UserId == userId);
            var dietPlan = allPlans.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            if (dietPlan == null) return ApiResponse<object>.FailureResult("Không tìm thấy thực đơn.");

            return ApiResponse<object>.SuccessResult(dietPlan);
        }

        public async Task<ApiResponse<object>> UpdateDietPlanAsync(string userId, UpdateDietPlanRequestDto request)
        {
            var allPlans = await _unitOfWork.Repository<DietPlan>().FindAsync(d => d.UserId == userId);
            var dietPlan = allPlans.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            if (dietPlan == null) return ApiResponse<object>.FailureResult("Không tìm thấy thực đơn để cập nhật.");

            dietPlan.DailyMealsJson = request.DailyMealsJson;
            dietPlan.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<DietPlan>().Update(dietPlan);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(dietPlan, "Cập nhật thực đơn thành công.");
        }

        public async Task<ApiResponse<object>> GenerateAiDietAsync(string userId, GenerateAiDietRequestDto request)
        {
            var allPlans = await _unitOfWork.Repository<DietPlan>().FindAsync(d => d.UserId == userId);
            var dietPlan = allPlans.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            if (dietPlan == null) return ApiResponse<object>.FailureResult("Không tìm thấy thực đơn để thêm món AI.");

            string aiResponseJson;
            try
            {
                aiResponseJson = await _geminiService.GenerateAiDietRecipeAsync(request.Query);
            }
            catch (Exception)
            {
                // GeminiService đã ghi log; không trả món mẫu thay cho kết quả thật.
                return ApiResponse<object>.FailureResult("AI hiện không thể gợi ý món. Vui lòng thử lại sau.");
            }

            return ApiResponse<object>.SuccessResult(new { aiMeal = aiResponseJson }, "Sinh món ăn AI thành công.");
        }
    }
}
