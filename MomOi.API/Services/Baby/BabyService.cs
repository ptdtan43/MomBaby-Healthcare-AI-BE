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

            // Đồng bộ menu hiện tại với bảng recipes (Category = Baby) theo TÊN MÓN:
            //  - Món đã có bản ghi (bất kể ngày nào) => GIỮ NGUYÊN trạng thái chuyên gia đã duyệt/từ chối
            //  - Món chưa có => tạo PendingReview để xuất hiện trong hàng chờ duyệt của chuyên gia
            // Sau đó gắn "status" (0=chờ, 1=duyệt, 2=từ chối) vào từng meal cho FE hiển thị badge.
            // Chạy trên MỌI lần gọi (kể cả cache-hit) để badge luôn phản ánh quyết định mới nhất.
            // Chạy trên MỌI lần gọi (kể cả cache-hit) để badge luôn phản ánh quyết định mới nhất.
            try
            {
                // Tự động làm sạch các Title bị ô nhiễm do debug suffix trong DB (Self-healing)
                var corruptedRecipes = await _recipeRepo.FindAsync(r => 
                    r.Category == RecipeCategory.Baby && 
                    r.Title != null &&
                    (r.Title.Contains(" (FOUND DB:") || r.Title.Contains(" (NEW DB:")));
                
                if (corruptedRecipes != null && corruptedRecipes.Count > 0)
                {
                    foreach (var cr in corruptedRecipes)
                    {
                        var idx = cr.Title.IndexOf(" (FOUND DB:");
                        if (idx == -1) idx = cr.Title.IndexOf(" (NEW DB:");
                        if (idx != -1)
                        {
                            cr.Title = cr.Title.Substring(0, idx).Trim();
                        }
                    }
                    await _recipeRepo.SaveChangesAsync();
                }

                var existingRecipes = await _recipeRepo.FindAsync(r =>
                    r.UserId == userId && r.Category == RecipeCategory.Baby);

                // Bản ghi mới nhất cho mỗi tên món (chuẩn hóa chữ thường, xóa khoảng trắng)
                var byTitle = existingRecipes
                    .GroupBy(r => (r.Title ?? "").Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.GeneratedAt).First());

                var jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(menu);
                var newRows = new List<MomOi.API.Models.Health.Recipe>();

                void SyncAndTag(System.Text.Json.Nodes.JsonObject mealsNode, string dayPrefix)
                {
                    foreach (var mealProp in mealsNode)
                    {
                        if (mealProp.Value is not System.Text.Json.Nodes.JsonObject mealNode) continue;

                        var rawMealName = mealNode["name_vi"]?.ToString()
                                       ?? mealNode["name_en"]?.ToString()
                                       ?? "Thực đơn bé";
                        
                        // Làm sạch mealName nếu bản thân cache cũng bị ô nhiễm bởi suffix
                        var mealName = rawMealName.Trim();
                        var debugIdx = mealName.IndexOf(" (FOUND DB:");
                        if (debugIdx == -1) debugIdx = mealName.IndexOf(" (NEW DB:");
                        if (debugIdx != -1)
                        {
                            mealName = mealName.Substring(0, debugIdx).Trim();
                            mealNode["name_vi"] = mealName; // Khôi phục lại tên gốc sạch sẽ
                        }

                        var title = string.IsNullOrEmpty(dayPrefix) ? mealName : $"{dayPrefix}: {mealName}";
                        var searchKey = title.Trim().ToLower();

                        if (!byTitle.TryGetValue(searchKey, out var recipeRow))
                        {
                            double cal = 0;
                            try { cal = mealNode["total_calories"]?.GetValue<double>() ?? 0; } catch { }

                            recipeRow = new MomOi.API.Models.Health.Recipe
                            {
                                UserId = userId,
                                ProfileStage = "post-natal",
                                Category = RecipeCategory.Baby,
                                Title = title, // Vẫn lưu title gốc sạch sẽ vào DB
                                Description = $"Tạo tự động từ AI dinh dưỡng cho bé {baby.AgeMonths} tháng tuổi.",
                                Calories = (int)cal,
                                Status = RecipeStatus.PendingReview,
                                GeneratedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            newRows.Add(recipeRow);
                            byTitle[searchKey] = recipeRow;
                        }

                        mealNode["status"] = (int)recipeRow.Status;
                    }
                }

                if (weekly)
                {
                    if (jsonNode?["days"] is System.Text.Json.Nodes.JsonArray daysNode)
                    {
                        foreach (var dayNode in daysNode)
                        {
                            var dName = dayNode?["day"]?.ToString() ?? "";
                            if (dayNode?["meals"] is System.Text.Json.Nodes.JsonObject weeklyMeals)
                                SyncAndTag(weeklyMeals, dName);
                        }
                    }
                }
                else if (jsonNode?["meals"] is System.Text.Json.Nodes.JsonObject dailyMeals)
                {
                    SyncAndTag(dailyMeals, "");
                }

                foreach (var r in newRows)
                {
                    await _recipeRepo.AddAsync(r);
                }
                if (newRows.Count > 0)
                {
                    await _recipeRepo.SaveChangesAsync();
                }

                menu = jsonNode!;
                _dailyMenuCache[cacheKey] = menu;
            }
            catch (Exception)
            {
                // Lỗi đồng bộ DB không nên chặn user xem thực đơn
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
