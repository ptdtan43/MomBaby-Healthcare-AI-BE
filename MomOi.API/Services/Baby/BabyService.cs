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

        public async Task<ApiResponse<object>> GetBabyMenuAsync(string userId, int babyId, bool weekly, bool forceRefresh = false)
        {
            var baby = await _babyRepo.FirstOrDefaultAsync(b => b.Id == babyId && b.UserId == userId);
            if (baby == null)
                return ApiResponse<object>.FailureResult("Không tìm thấy hồ sơ bé.");

            var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string cacheKey = weekly ? $"weekly_{userId}_{babyId}_{todayStr}" : $"daily_{userId}_{babyId}_{todayStr}";

            object menu = null;
            bool isNewMenu = false;

            if (!forceRefresh && _dailyMenuCache.TryGetValue(cacheKey, out var cachedMenu))
            {
                menu = cachedMenu;
            }
            else
            {
                menu = weekly
                    ? await _nutritionProxy.GetBabyWeeklyMenuAsync(baby.AgeMonths, baby.CurrentWeightKg, baby.Allergies)
                    : await _nutritionProxy.GetBabyDailyMenuAsync(baby.AgeMonths, baby.CurrentWeightKg, baby.Allergies);
                isNewMenu = true;
            }

            if (menu == null)
                return ApiResponse<object>.FailureResult(
                    "Dịch vụ dinh dưỡng (Nutrition API) hiện không khả dụng. Vui lòng thử lại sau.");

            // Thêm logic tự động lưu vào Database để chuyên gia có thể duyệt và ánh xạ trạng thái
            if (menu != null)
            {
                try
                {
                    var today = DateTime.UtcNow.Date;
                    var existingRecipes = await _recipeRepo.FindAsync(r => r.UserId == userId && r.Category == RecipeCategory.Baby && r.GeneratedAt >= today);
                    
                    var existingList = existingRecipes.ToList();
                    
                    // If we are force refreshing, we want to clear the old recipes and save the new ones
                    if (isNewMenu && forceRefresh && existingList.Any())
                    {
                        foreach (var r in existingList)
                        {
                            _recipeRepo.Remove(r);
                        }
                        await _recipeRepo.SaveChangesAsync();
                        existingList.Clear();
                    }
                    
                    // Also clear existing if we are fetching weekly but only daily exists, or vice versa?
                    // Actually, if we just want to ensure we save at least once, we can just save if !existingList.Any()
                    if (isNewMenu && !existingList.Any())
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(menu);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        
                        var recipesToAdd = new List<MomOi.API.Models.Health.Recipe>();
                        
                        void AddMeals(System.Text.Json.JsonElement meals, string dayPrefix)
                        {
                            foreach (var mealProp in meals.EnumerateObject())
                            {
                                var mealObj = mealProp.Value;
                                var mealName = mealObj.TryGetProperty("name_vi", out var nameEl) ? nameEl.GetString() : "Thực đơn cho bé";
                                var calories = mealObj.TryGetProperty("total_calories", out var calEl) ? calEl.GetSingle() : 0;
                                
                                var newRecipe = new MomOi.API.Models.Health.Recipe
                                {
                                    UserId = userId,
                                    ProfileStage = "post-natal",
                                    Category = RecipeCategory.Baby,
                                    Title = string.IsNullOrEmpty(dayPrefix) ? (mealName ?? "Thực đơn bé") : $"{dayPrefix}: {mealName ?? "Thực đơn bé"}",
                                    Description = $"Tạo tự động từ AI dinh dưỡng cho bé {baby.AgeMonths} tháng tuổi.",
                                    Calories = (int)calories,
                                    Status = RecipeStatus.PendingReview,
                                    GeneratedAt = DateTime.UtcNow,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                recipesToAdd.Add(newRecipe);
                                existingList.Add(newRecipe);
                            }
                        }

                        if (weekly)
                        {
                            if (doc.RootElement.TryGetProperty("days", out var days))
                            {
                                foreach (var day in days.EnumerateArray())
                                {
                                    var dayName = day.TryGetProperty("day", out var dName) ? dName.GetString() : "";
                                    if (day.TryGetProperty("meals", out var meals))
                                    {
                                        AddMeals(meals, dayName);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (doc.RootElement.TryGetProperty("meals", out var meals))
                            {
                                AddMeals(meals, "");
                            }
                        }

                        foreach (var r in recipesToAdd)
                        {
                            await _recipeRepo.AddAsync(r);
                        }
                        await _recipeRepo.SaveChangesAsync();
                    }

                    // Map status to the menu object
                    var jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(menu);
                    
                    if (weekly && jsonNode?["days"] is System.Text.Json.Nodes.JsonArray daysNode)
                    {
                        foreach (var dayNode in daysNode)
                        {
                            var dName = dayNode?["day"]?.ToString() ?? "";
                            if (dayNode?["meals"] is System.Text.Json.Nodes.JsonObject mealsNode)
                            {
                                foreach (var mealProp in mealsNode)
                                {
                                    var mealName = mealProp.Value?["name_vi"]?.ToString();
                                    var fullTitle = string.IsNullOrEmpty(dName) ? mealName : $"{dName}: {mealName}";
                                    var matchingRecipe = existingList.FirstOrDefault(r => r.Title == fullTitle);
                                    if (matchingRecipe != null && mealProp.Value is System.Text.Json.Nodes.JsonObject singleMealNode)
                                    {
                                        singleMealNode["status"] = (int)matchingRecipe.Status;
                                    }
                                }
                            }
                        }
                    }
                    else if (!weekly && jsonNode?["meals"] is System.Text.Json.Nodes.JsonObject mealsNode)
                    {
                        foreach (var mealProp in mealsNode)
                        {
                            var mealName = mealProp.Value?["name_vi"]?.ToString();
                            var matchingRecipe = existingList.FirstOrDefault(r => r.Title == mealName);
                            if (matchingRecipe != null && mealProp.Value is System.Text.Json.Nodes.JsonObject singleMealNode)
                            {
                                singleMealNode["status"] = (int)matchingRecipe.Status;
                            }
                        }
                    }
                    
                    // Return the modified JSON and update cache
                    menu = jsonNode;
                    _dailyMenuCache[cacheKey] = menu;
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

            var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            _dailyMenuCache.TryRemove($"daily_{userId}_{id}_{todayStr}", out _);
            _dailyMenuCache.TryRemove($"weekly_{userId}_{id}_{todayStr}", out _);

            return ApiResponse<BabyProfile>.SuccessResult(existing, "Cập nhật hồ sơ bé thành công.");
        }
    }
}
