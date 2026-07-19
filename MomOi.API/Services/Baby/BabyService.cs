using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.BusinessRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Baby
{
    public class BabyService : IBabyService
    {
        private readonly IGenericRepository<BabyProfile> _babyRepo;
        private readonly IGenericRepository<GrowthRecord> _growthRepo;
        private readonly IBusinessRuleEngine _businessRuleEngine;
        private readonly Nutrition.NutritionProxyService _nutritionProxy;
        private readonly IGenericRepository<MomOi.API.Models.Health.Recipe> _recipeRepo;

        public BabyService(
            IGenericRepository<BabyProfile> babyRepo,
            IGenericRepository<GrowthRecord> growthRepo,
            IBusinessRuleEngine businessRuleEngine,
            Nutrition.NutritionProxyService nutritionProxy,
            IGenericRepository<MomOi.API.Models.Health.Recipe> recipeRepo)
        {
            _babyRepo = babyRepo;
            _growthRepo = growthRepo;
            _businessRuleEngine = businessRuleEngine;
            _nutritionProxy = nutritionProxy;
            _recipeRepo = recipeRepo;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _dailyMenuCache = new();

        public async Task<ApiResponse<object>> GetBabyMenuAsync(string userId, int babyId, bool weekly)
        {
            var baby = await _babyRepo.FirstOrDefaultAsync(b => b.Id == babyId && b.UserId == userId);
            if (baby == null)
                return ApiResponse<object>.FailureResult("Không tìm thấy hồ sơ bé.");

            string cacheKey = "";
            if (!weekly)
            {
                var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
                cacheKey = $"{userId}_{babyId}_{todayStr}";
                if (_dailyMenuCache.TryGetValue(cacheKey, out var cachedMenu))
                {
                    return ApiResponse<object>.SuccessResult(cachedMenu, "Lấy thực đơn cho bé thành công.");
                }
            }

            var menu = weekly
                ? await _nutritionProxy.GetBabyWeeklyMenuAsync(baby.AgeMonths, baby.CurrentWeightKg, baby.Allergies)
                : await _nutritionProxy.GetBabyDailyMenuAsync(baby.AgeMonths, baby.CurrentWeightKg, baby.Allergies);

            if (menu == null)
                return ApiResponse<object>.FailureResult(
                    "Dịch vụ dinh dưỡng (Nutrition API) hiện không khả dụng. Vui lòng thử lại sau.");

            // Cập nhật cache nếu là daily menu
            if (!weekly && menu != null)
            {
                _dailyMenuCache[cacheKey] = menu;
            }

            // Thêm logic tự động lưu vào Database để chuyên gia có thể duyệt
            if (!weekly && menu != null)
            {
                try
                {
                    var today = DateTime.UtcNow.Date;
                    var existingRecipes = await _recipeRepo.FindAsync(r => r.UserId == userId && r.Category == RecipeCategory.Baby && r.GeneratedAt.Date == today);
                    if (!existingRecipes.Any())
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(menu);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("meals", out var meals))
                        {
                            foreach (var mealProp in meals.EnumerateObject())
                            {
                                var mealObj = mealProp.Value;
                                var mealName = mealObj.TryGetProperty("name_vi", out var nameEl) ? nameEl.GetString() : "Thực đơn cho bé";
                                var calories = mealObj.TryGetProperty("total_calories", out var calEl) ? calEl.GetSingle() : 0;
                                
                                await _recipeRepo.AddAsync(new MomOi.API.Models.Health.Recipe
                                {
                                    UserId = userId,
                                    ProfileStage = "post-natal",
                                    Category = RecipeCategory.Baby,
                                    Title = mealName ?? "Thực đơn bé",
                                    Description = $"Tạo tự động từ AI dinh dưỡng cho bé {baby.AgeMonths} tháng tuổi.",
                                    Calories = (int)calories,
                                    Status = RecipeStatus.PendingReview,
                                    GeneratedAt = DateTime.UtcNow,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                            await _recipeRepo.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception)
                {
                    // Lỗi lưu DB không nên chặn user xem thực đơn
                }
            }

            return ApiResponse<object>.SuccessResult(menu, "Lấy thực đơn cho bé thành công.");
        }

        public async Task<ApiResponse<BabyProfile>> CreateBabyProfileAsync(string userId, BabyProfile profile)
        {
            profile.UserId = userId;
            await _babyRepo.AddAsync(profile);
            await _babyRepo.SaveChangesAsync();

            return ApiResponse<BabyProfile>.SuccessResult(profile, "Tạo hồ sơ cho bé thành công.");
        }

        public async Task<ApiResponse<List<BabyProfile>>> GetBabyProfilesAsync(string userId)
        {
            var profiles = (await _babyRepo.FindAsync(p => p.UserId == userId)).ToList();
            var profileIds = profiles.Select(p => p.Id).ToList();
            var growthRecords = (await _growthRepo.FindAsync(g => profileIds.Contains(g.BabyProfileId))).ToList();

            foreach (var profile in profiles)
            {
                profile.GrowthRecords = growthRecords.Where(g => g.BabyProfileId == profile.Id).ToList();
            }

            return ApiResponse<List<BabyProfile>>.SuccessResult(profiles);
        }

        public async Task<ApiResponse<GrowthEvaluationResult>> LogGrowthAsync(string userId, int babyId, GrowthRecord record)
        {
            var baby = await _babyRepo.FirstOrDefaultAsync(b => b.Id == babyId && b.UserId == userId);
            if (baby == null)
            {
                return ApiResponse<GrowthEvaluationResult>.FailureResult("Không tìm thấy hồ sơ của bé.");
            }

            record.BabyProfileId = baby.Id;
            record.BabyProfile = null;
            if (record.RecordedAt == default)
            {
                record.RecordedAt = DateTime.UtcNow;
            }

            await _growthRepo.AddAsync(record);

            // Cập nhật chỉ số hiện tại của bé dựa trên mốc mới nhất (theo RecordedAt)
            var allRecords = (await _growthRepo.FindAsync(g => g.BabyProfileId == babyId)).ToList();
            allRecords.Add(record);
            var latestRecord = allRecords.OrderByDescending(g => g.RecordedAt).First();

            baby.CurrentWeightKg = latestRecord.WeightKg;
            baby.CurrentHeightCm = latestRecord.HeightCm;

            _babyRepo.Update(baby);
            await _babyRepo.SaveChangesAsync();

            var evaluation = _businessRuleEngine.VerifyBabyGrowth(
                Math.Max(0, (int)((record.RecordedAt - baby.DateOfBirth).TotalDays / 30.44)),
                baby.Gender.ToString(),
                record.WeightKg,
                record.HeightCm
            );

            return ApiResponse<GrowthEvaluationResult>.SuccessResult(evaluation, "Ghi nhận chỉ số tăng trưởng và đánh giá thành công.");
        }

        public async Task<ApiResponse<object>> DeleteGrowthRecordAsync(string userId, int babyId, int recordId)
        {
            var baby = await _babyRepo.FirstOrDefaultAsync(b => b.Id == babyId && b.UserId == userId);
            if (baby == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy hồ sơ của bé.");
            }

            var record = await _growthRepo.FirstOrDefaultAsync(g => g.Id == recordId && g.BabyProfileId == babyId);
            if (record == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy chỉ số tăng trưởng.");
            }

            _growthRepo.Remove(record);
            await _growthRepo.SaveChangesAsync();

            // Cập nhật lại cân nặng/chiều cao hiện tại của bé dựa trên chỉ số mới nhất còn lại
            var remainingRecords = (await _growthRepo.FindAsync(g => g.BabyProfileId == babyId))
                .OrderByDescending(g => g.RecordedAt)
                .ToList();

            if (remainingRecords.Any())
            {
                var latest = remainingRecords.First();
                baby.CurrentWeightKg = latest.WeightKg;
                baby.CurrentHeightCm = latest.HeightCm;
            }
            else
            {
                baby.CurrentWeightKg = null;
                baby.CurrentHeightCm = null;
            }

            _babyRepo.Update(baby);
            await _babyRepo.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(null!, "Xóa chỉ số tăng trưởng thành công.");
        }

        public async Task<ApiResponse<BabyProfile>> UpdateBabyProfileAsync(string userId, int id, BabyProfile profile)
        {
            var existing = await _babyRepo.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (existing == null)
            {
                return ApiResponse<BabyProfile>.FailureResult("Không tìm thấy hồ sơ của bé.");
            }

            existing.BabyName = profile.BabyName;
            existing.DateOfBirth = profile.DateOfBirth;
            existing.Gender = profile.Gender;
            existing.CurrentWeightKg = profile.CurrentWeightKg;
            existing.CurrentHeightCm = profile.CurrentHeightCm;
            existing.Allergies = profile.Allergies;
            existing.FoodHistory = profile.FoodHistory;

            _babyRepo.Update(existing);
            await _babyRepo.SaveChangesAsync();

            return ApiResponse<BabyProfile>.SuccessResult(existing, "Cập nhật hồ sơ bé thành công.");
        }
    }
}
