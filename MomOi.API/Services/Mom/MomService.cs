using Microsoft.AspNetCore.Identity;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Mom;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Models.Nutrition;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Mom
{
    public class MomService : IMomService
    {
        private readonly IUnitOfWork _unitOfWork;
        // UserManager là đặc thù của ASP.NET Identity, không thể thay bằng Repository
        private readonly UserManager<AppUser> _userManager;
        private readonly IGeminiService _geminiService;

        public MomService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IGeminiService geminiService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _geminiService = geminiService;
        }

        // ─── Allergies ──────────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetAllergiesAsync(string userId)
        {
            var allergies = await _unitOfWork.Repository<FoodAllergyRecord>()
                .FindAsync(a => a.UserId == userId);

            var result = allergies.Select(a => new AllergyResponseDto
            {
                Id = a.Id,
                Allergen = a.Allergen,
                Severity = a.Severity.ToString(),
                Symptoms = a.Symptoms
            }).ToList();

            return ApiResponse<object>.SuccessResult(result, "Lấy danh sách dị ứng thành công.");
        }

        public async Task<ApiResponse<object>> AddAllergyAsync(string userId, CreateAllergyDto dto)
        {
            var allergy = new FoodAllergyRecord
            {
                UserId = userId,
                Allergen = dto.Allergen,
                Severity = Enum.TryParse<AllergySeverity>(dto.Severity, true, out var pSev) ? pSev : AllergySeverity.Mild,
                Symptoms = dto.Symptoms
            };

            await _unitOfWork.Repository<FoodAllergyRecord>().AddAsync(allergy);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = new AllergyResponseDto
            {
                Id = allergy.Id,
                Allergen = allergy.Allergen,
                Severity = allergy.Severity.ToString(),
                Symptoms = allergy.Symptoms
            };

            return ApiResponse<object>.SuccessResult(responseDto, "Thêm thông tin dị ứng thành công.");
        }

        public async Task<ApiResponse<object>> RemoveAllergyAsync(string userId, int allergyId)
        {
            var allergy = await _unitOfWork.Repository<FoodAllergyRecord>()
                .FirstOrDefaultAsync(a => a.Id == allergyId && a.UserId == userId);

            if (allergy == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy thông tin dị ứng này.");
            }

            _unitOfWork.Repository<FoodAllergyRecord>().Remove(allergy);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult((object)"OK", "Đã xóa thông tin dị ứng.");
        }

        // ─── Diet Plans ─────────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetDietPlansAsync(string userId)
        {
            var plansFromDb = await _unitOfWork.Repository<DietPlan>()
                .FindAsync(d => d.UserId == userId);

            var plans = plansFromDb.OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id, d.WeekNumber, d.GeneratedFrom, d.CreatedAt,
                    DailyMeals = string.IsNullOrWhiteSpace(d.DailyMealsJson)
                        ? (object?)null
                        : JsonDocument.Parse(d.DailyMealsJson).RootElement
                }).ToList();

            return ApiResponse<object>.SuccessResult(plans, "Lấy danh sách thực đơn thành công.");
        }

        public async Task<ApiResponse<object>> CreateManualDietPlanAsync(string userId, CreateDietPlanDto dto)
        {
            var plan = new DietPlan
            {
                UserId = userId,
                WeekNumber = dto.WeekNumber,
                DailyMealsJson = dto.DailyMealsJson,
                GeneratedFrom = DietPlanSource.Manual,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<DietPlan>().AddAsync(plan);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult((object)plan.Id, "Tạo thực đơn thủ công thành công.");
        }

        public async Task<ApiResponse<object>> GenerateAIDietPlanAsync(string userId, GenerateDietPlanDto dto)
        {
            // 1. Get user allergies to include in the AI prompt
            var allergyRecords = await _unitOfWork.Repository<FoodAllergyRecord>()
                .FindAsync(a => a.UserId == userId);
            var allergies = allergyRecords.Select(a => a.Allergen).ToList();

            string allergyContext = allergies.Any()
                ? $"Trẻ bị dị ứng với: {string.Join(", ", allergies)}. TUYỆT ĐỐI KHÔNG ĐƯA CÁC MÓN NÀY VÀO THỰC ĐƠN."
                : "Trẻ không có dị ứng thức ăn nào.";

            string prompt = $@"
Hãy tạo một thực đơn 7 ngày dành cho trẻ {dto.BabyAgeInMonths} tháng tuổi, nặng {dto.BabyWeightKg} kg.
{allergyContext}
Lưu ý bổ sung: {dto.AdditionalNotes}

YÊU CẦU ĐẦU RA (Chỉ trả về chuỗi JSON Array nguyên bản, KHÔNG giải thích, KHÔNG có markdown):
[
  {{
    ""day"": ""Ngày 1"",
    ""meals"": [
      {{
        ""mealType"": ""Sáng"",
        ""recipe"": ""Tên món ăn"",
        ""calories"": 200
      }}
    ]
  }}
]";

            string aiResponseJson;
            try
            {
                aiResponseJson = await _geminiService.GenerateJsonAsync(
                    "Bạn là chuyên gia dinh dưỡng nhi khoa hàng đầu.\n\n" + prompt);
                JsonDocument.Parse(aiResponseJson); // validate before persisting
            }
            catch (Exception)
            {
                // No fabricated fallback plan: fail honestly so the client can retry.
                return ApiResponse<object>.FailureResult(
                    "AI hiện không thể tạo thực đơn. Vui lòng thử lại sau.");
            }

            var plan = new DietPlan
            {
                UserId = userId,
                WeekNumber = dto.WeekNumber,
                DailyMealsJson = aiResponseJson,
                GeneratedFrom = DietPlanSource.AiGenerated,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<DietPlan>().AddAsync(plan);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult((object)plan.Id, "Đã tạo thực đơn bằng AI thành công.");
        }

        // ─── Premium Upgrade ────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> UpgradeToPremiumAsync(string userId, UpgradePremiumDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy người dùng.");
            }

            if (string.IsNullOrWhiteSpace(dto.TransactionId))
            {
                return ApiResponse<object>.FailureResult("Thiếu mã giao dịch thanh toán.");
            }

            user.Tier = SubscriptionTier.SuperMomVip;
            user.TierExpiresAt = DateTime.UtcNow.AddMonths(dto.MonthsToUpgrade);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ApiResponse<object>.FailureResult("Lỗi khi cập nhật tài khoản.");
            }

            return ApiResponse<object>.SuccessResult((object)new
            {
                user.Tier,
                user.TierExpiresAt
            }, "Nâng cấp lên SuperMom VIP thành công.");
        }
    }
}
