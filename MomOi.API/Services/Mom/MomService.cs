using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Mom;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Models.Nutrition;
using MomOi.API.Services.AI;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Mom
{
    public class MomService : IMomService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IGeminiService _geminiService;

        public MomService(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IGeminiService geminiService)
        {
            _context = context;
            _userManager = userManager;
            _geminiService = geminiService;
        }

        // ─── Allergies ──────────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetAllergiesAsync(string userId)
        {
            var allergies = await _context.FoodAllergyRecords
                .Where(a => a.UserId == userId)
                .Select(a => new AllergyResponseDto
                {
                    Id = a.Id,
                    Allergen = a.Allergen,
                    Severity = a.Severity,
                    Symptoms = a.Symptoms
                })
                .ToListAsync();

            return ApiResponse<object>.SuccessResult(allergies, "Lấy danh sách dị ứng thành công.");
        }

        public async Task<ApiResponse<object>> AddAllergyAsync(string userId, CreateAllergyDto dto)
        {
            var allergy = new FoodAllergyRecord
            {
                UserId = userId,
                Allergen = dto.Allergen,
                Severity = dto.Severity,
                Symptoms = dto.Symptoms
            };

            _context.FoodAllergyRecords.Add(allergy);
            await _context.SaveChangesAsync();

            var responseDto = new AllergyResponseDto
            {
                Id = allergy.Id,
                Allergen = allergy.Allergen,
                Severity = allergy.Severity,
                Symptoms = allergy.Symptoms
            };

            return ApiResponse<object>.SuccessResult(responseDto, "Thêm thông tin dị ứng thành công.");
        }

        public async Task<ApiResponse<object>> RemoveAllergyAsync(string userId, int allergyId)
        {
            var allergy = await _context.FoodAllergyRecords
                .FirstOrDefaultAsync(a => a.Id == allergyId && a.UserId == userId);

            if (allergy == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy thông tin dị ứng này.");
            }

            _context.FoodAllergyRecords.Remove(allergy);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult((object)"OK", "Đã xóa thông tin dị ứng.");
        }

        // ─── Diet Plans ─────────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetDietPlansAsync(string userId)
        {
            var plansFromDb = await _context.DietPlans
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.WeekNumber,
                    d.GeneratedFrom,
                    d.CreatedAt,
                    d.DailyMealsJson
                })
                .ToListAsync();

            var plans = plansFromDb.Select(d => new
            {
                d.Id,
                d.WeekNumber,
                d.GeneratedFrom,
                d.CreatedAt,
                DailyMeals = string.IsNullOrWhiteSpace(d.DailyMealsJson) ? (object?)null : JsonDocument.Parse(d.DailyMealsJson).RootElement
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
                GeneratedFrom = "manual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DietPlans.Add(plan);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult((object)plan.Id, "Tạo thực đơn thủ công thành công.");
        }

        public async Task<ApiResponse<object>> GenerateAIDietPlanAsync(string userId, GenerateDietPlanDto dto)
        {
            // 1. Get user allergies to include in the AI prompt
            var allergies = await _context.FoodAllergyRecords
                .Where(a => a.UserId == userId)
                .Select(a => a.Allergen)
                .ToListAsync();

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
            
            string aiResponseJson = "";
            try
            {
                aiResponseJson = await _geminiService.SendChatMessageAsync(prompt, "Bạn là chuyên gia dinh dưỡng nhi khoa hàng đầu.");
                
                // Clean up potential markdown formatting from Gemini response
                aiResponseJson = aiResponseJson.Replace("```json", "").Replace("```", "").Trim();
                
                // Validate if it's actual JSON
                var jsonDoc = JsonDocument.Parse(aiResponseJson);
            }
            catch (Exception ex)
            {
                // Fallback to static mock JSON if Gemini fails or API key is not configured
                aiResponseJson = @"[
                  {
                    ""day"": ""Ngày 1"",
                    ""meals"": [
                      { ""mealType"": ""Sáng"", ""recipe"": ""Cháo yến mạch cá hồi"", ""calories"": 180 },
                      { ""mealType"": ""Trưa"", ""recipe"": ""Súp bí đỏ thịt băm"", ""calories"": 250 },
                      { ""mealType"": ""Tối"", ""recipe"": ""Cơm nát cá kho tộ"", ""calories"": 220 }
                    ]
                  }
                ]";
            }

            var plan = new DietPlan
            {
                UserId = userId,
                WeekNumber = dto.WeekNumber,
                DailyMealsJson = aiResponseJson,
                GeneratedFrom = "ai-generated",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DietPlans.Add(plan);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult((object)plan.Id, "Đã tạo thực đơn bằng AI (hoặc dự phòng) thành công.");
        }

        // ─── Premium Upgrade ────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> UpgradeToPremiumAsync(string userId, UpgradePremiumDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy người dùng.");
            }

            // Mocking payment verification...
            if (string.IsNullOrWhiteSpace(dto.TransactionId))
            {
                return ApiResponse<object>.FailureResult("Thiếu mã giao dịch thanh toán.");
            }

            // Update Tier
            user.Tier = SubscriptionTier.SuperMomVip;
            user.TierExpiresAt = DateTime.UtcNow.AddMonths(dto.MonthsToUpgrade);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return ApiResponse<object>.FailureResult("Lỗi khi cập nhật tài khoản.");
            }

            return ApiResponse<object>.SuccessResult((object)new { 
                user.Tier, 
                user.TierExpiresAt 
            }, "Nâng cấp lên SuperMom VIP thành công.");
        }
    }
}
