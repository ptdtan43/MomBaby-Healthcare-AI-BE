using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Models.Nutrition;
using MomOi.API.Repositories;
using MomOi.API.Services.BusinessRules;
using MomOi.API.Services.Nutrition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Pregnancy
{
    public class PregnancyService : IPregnancyService
    {
        private readonly IGenericRepository<MomHealthProfile> _profileRepo;
        private readonly IGenericRepository<MealLog> _mealRepo;
        private readonly IGenericRepository<PregnancyLog> _pregLogRepo;
        private readonly IGenericRepository<ExerciseLog> _exerciseRepo;
        private readonly IBusinessRuleEngine _ruleEngine;
        private readonly NutritionProxyService _nutritionProxy;

        public PregnancyService(
            IGenericRepository<MomHealthProfile> profileRepo,
            IGenericRepository<MealLog> mealRepo,
            IGenericRepository<PregnancyLog> pregLogRepo,
            IGenericRepository<ExerciseLog> exerciseRepo,
            IBusinessRuleEngine ruleEngine,
            NutritionProxyService nutritionProxy)
        {
            _profileRepo = profileRepo;
            _mealRepo = mealRepo;
            _pregLogRepo = pregLogRepo;
            _exerciseRepo = exerciseRepo;
            _ruleEngine = ruleEngine;
            _nutritionProxy = nutritionProxy;
        }

        public async Task<ApiResponse<object>> SetupPregnancyAsync(string userId, DateTime lastMenstrualPeriod, DateTime? dueDate)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            
            if (profile == null)
            {
                profile = new MomHealthProfile
                {
                    UserId = userId,
                    Stage = JourneyStage.Pregnant,
                    LastPeriodDate = lastMenstrualPeriod,
                    UpdatedAt = DateTime.UtcNow
                };
                await _profileRepo.AddAsync(profile);
            }
            else
            {
                profile.Stage = JourneyStage.Pregnant;
                profile.LastPeriodDate = lastMenstrualPeriod;
                profile.UpdatedAt = DateTime.UtcNow;
                _profileRepo.Update(profile);
            }

            var daysElapsed = (DateTime.UtcNow - lastMenstrualPeriod).Days;
            var week = (daysElapsed / 7) + 1;
            if (week < 1) week = 1;
            if (week > 42) week = 42;

            var calcDueDate = dueDate ?? lastMenstrualPeriod.AddDays(280);

            profile.PregnancyWeek = week;
            profile.DeliveryDate = calcDueDate;
            await _profileRepo.SaveChangesAsync();

            var trimester = week <= 12 ? 1 : (week <= 27 ? 2 : 3);
            var milestone = $"Tuần {week}: Bé đang hình thành và phát triển tích cực.";

            var result = new
            {
                PregnancyWeek = week,
                Trimester = trimester,
                DueDate = calcDueDate,
                Milestone = milestone
            };

            return ApiResponse<object>.SuccessResult(result, "Thiết lập trạng thái thai sản thành công.");
        }

        public async Task<ApiResponse<object>> GetThisWeekAsync(string userId)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || profile.Stage != JourneyStage.Pregnant || !profile.PregnancyWeek.HasValue)
            {
                return ApiResponse<object>.FailureResult("Hồ sơ hiện tại không ở chế độ mang thai. Vui lòng thiết lập trước.");
            }

            int week = profile.PregnancyWeek.Value;
            var milestoneData = GetMilestoneForWeek(week);

            return ApiResponse<object>.SuccessResult(milestoneData);
        }

        public async Task<ApiResponse<object>> LogFoodAsync(string userId, string[] foods)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return ApiResponse<object>.FailureResult("Hồ sơ sức khỏe không tồn tại.");

            var mealLog = new MealLog
            {
                UserId = userId,
                LoggedAt = DateTime.UtcNow,
                MealType = MealType.Snack,
                FoodItems = foods,
                Calories = 250f
            };
            await _mealRepo.AddAsync(mealLog);
            await _mealRepo.SaveChangesAsync();

            var alerts = await _ruleEngine.EvaluateAsync(profile);
            var br02Alerts = alerts.Where(a => a.RuleId == "BR02").ToList();

            var safeAlternatives = new List<string>();
            if (br02Alerts.Any())
            {
                safeAlternatives.Add("Phở bò chín kỹ");
                safeAlternatives.Add("Kimbap chín (không dùng trứng sống/cá sống)");
                safeAlternatives.Add("Sữa chua tiệt trùng kèm trái cây chín (chuối, xoài chín)");
            }

            var result = new
            {
                Alerts = br02Alerts,
                SafeAlternatives = safeAlternatives
            };

            return ApiResponse<object>.SuccessResult(result, "Ghi chép bữa ăn thành công.");
        }

        public async Task<ApiResponse<object>> GetMealPlanAsync(string userId, int? week)
        {
            int selectedWeek = week ?? 12;

            // The 7-day plan (dishes + USDA-computed nutrient totals) comes from the
            // Python nutrition engine. No hardcoded fallback: if the service is down
            // we report it honestly instead of showing fabricated data.
            var apiResult = await _nutritionProxy.GetMealPlanAsync(selectedWeek);

            if (apiResult == null)
            {
                return ApiResponse<object>.FailureResult(
                    "Dịch vụ dinh dưỡng (Nutrition API) hiện không khả dụng. Vui lòng thử lại sau.");
            }

            return ApiResponse<object>.SuccessResult(apiResult, "Lấy thực đơn thai kỳ thành công.");
        }

        public async Task<ApiResponse<object>> LogWeightAsync(string userId, float weightKg, DateTime date)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return ApiResponse<object>.FailureResult("Hồ sơ sức khỏe không tồn tại.");

            var pregnancyLog = new PregnancyLog
            {
                UserId = userId,
                Week = profile.PregnancyWeek ?? 12,
                Weight = weightKg,
                RecordedAt = date
            };
            await _pregLogRepo.AddAsync(pregnancyLog);

            profile.Bmi = weightKg / 2.5f;
            profile.UpdatedAt = DateTime.UtcNow;
            _profileRepo.Update(profile);

            await _profileRepo.SaveChangesAsync();

            var alerts = await _ruleEngine.EvaluateAsync(profile);
            var br04Alert = alerts.FirstOrDefault(a => a.RuleId == "BR04");

            var weightLogs = await _pregLogRepo.FindAsync(p => p.UserId == userId && p.Weight.HasValue);
            var firstWeightLog = weightLogs.OrderBy(p => p.RecordedAt).FirstOrDefault();

            float totalGain = 0f;
            if (firstWeightLog != null)
            {
                totalGain = weightKg - firstWeightLog.Weight!.Value;
            }

            var result = new
            {
                WeeklyGain = br04Alert != null ? -1f : 0.4f,
                TotalGain = totalGain,
                Recommendation = br04Alert != null ? br04Alert.SuggestionVi : "Tốc độ tăng cân tốt mami nhé! Hãy duy trì vận động nhẹ nhàng."
            };

            return ApiResponse<object>.SuccessResult(result, "Ghi chép cân nặng thành công.");
        }

        public async Task<ApiResponse<object>> GetExercisePlanAsync(string userId)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || profile.Stage != JourneyStage.Pregnant || !profile.PregnancyWeek.HasValue)
            {
                return ApiResponse<object>.FailureResult("Hồ sơ không ở chế độ thai kỳ để tính toán bài tập phù hợp.");
            }

            int week = profile.PregnancyWeek.Value;
            object plan;

            if (week <= 12)
            {
                plan = new
                {
                    Trimester = 1,
                    Exercises = new[]
                    {
                        new { Name = "Yoga xoay hông nhẹ nhàng", Reps = "8 nhịp", Duration = "5 phút" },
                        new { Name = "Căng giãn cơ cổ và vai gáy", Reps = "5 nhịp", Duration = "3 phút" },
                        new { Name = "Đi bộ chậm tại chỗ", Reps = "Tự do", Duration = "10 phút" }
                    }
                };
            }
            else if (week <= 27)
            {
                plan = new
                {
                    Trimester = 2,
                    Exercises = new[]
                    {
                        new { Name = "Squat nhẹ tựa tường", Reps = "10 lần", Duration = "5 phút" },
                        new { Name = "Bơi ếch nhịp nhàng", Reps = "Tự do", Duration = "20 phút" },
                        new { Name = "Pilates tăng cường xương chậu", Reps = "8 lần", Duration = "10 phút" }
                    }
                };
            }
            else
            {
                plan = new
                {
                    Trimester = 3,
                    Exercises = new[]
                    {
                        new { Name = "Hít sâu thở chậm chéo cánh tay", Reps = "12 lần", Duration = "5 phút" },
                        new { Name = "Đi bộ nhẹ ngoài trời", Reps = "Tự do", Duration = "15 phút" },
                        new { Name = "Tư thế con mèo con bò thư giãn lưng", Reps = "5 lần", Duration = "5 phút" }
                    }
                };
            }

            return ApiResponse<object>.SuccessResult(plan);
        }

        public async Task<ApiResponse<object>> LogExerciseAsync(string userId, int stepCount, string exerciseType, int durationMinutes)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return ApiResponse<object>.FailureResult("Hồ sơ sức khỏe không tồn tại.");

            var log = new ExerciseLog
            {
                UserId = userId,
                StepCount = stepCount,
                ExerciseType = exerciseType,
                DurationMinutes = durationMinutes,
                RecordedAt = DateTime.UtcNow
            };
            await _exerciseRepo.AddAsync(log);
            await _exerciseRepo.SaveChangesAsync();

            var alerts = await _ruleEngine.EvaluateAsync(profile);
            var br03Alerts = alerts.Where(a => a.RuleId == "BR03").ToList();

            return ApiResponse<object>.SuccessResult(new { Alerts = br03Alerts }, "Ghi nhận vận động thành công.");
        }

        public async Task<ApiResponse<object>> GetWeightLogsAsync(string userId)
        {
            var logs = await _pregLogRepo.FindAsync(p => p.UserId == userId && p.Weight.HasValue);
            var sortedLogs = logs.OrderBy(l => l.RecordedAt)
                                 .Select(l => new {
                                     week = l.Week,
                                     weight = l.Weight,
                                     recordedAt = l.RecordedAt
                                 }).ToList();
            return ApiResponse<object>.SuccessResult(sortedLogs);
        }

        public async Task<ApiResponse<object>> GetTodayStepsAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            var logs = await _exerciseRepo.FindAsync(e => e.UserId == userId && e.RecordedAt >= today);
            int totalSteps = logs.Sum(e => e.StepCount);
            return ApiResponse<object>.SuccessResult(new { todaySteps = totalSteps });
        }

        private object GetMilestoneForWeek(int week)
        {
            string sizeStr = "bằng quả chanh ta";
            string devStr = "Hệ thống thần kinh và các cơ quan nội tạng chính bắt đầu hình thành sơ khai.";
            string tipStr = "Uống nhiều nước, bổ sung Axit Folic đầy đủ và chia nhỏ bữa ăn để hạn chế ốm nghén.";

            if (week >= 9 && week <= 12)
            {
                sizeStr = "bằng quả chanh lớn 🍋";
                devStr = "Bé đã có thể cử động ngón tay nhỏ xíu và mí mắt đã nhắm lại.";
                tipStr = "Thực hiện siêu âm đo độ mờ da gáy trong khoảng tuần 11-13.";
            }
            else if (week >= 13 && week <= 20)
            {
                sizeStr = "bằng quả xoài chín 🥭";
                devStr = "Bé bắt đầu nghe được âm thanh nhịp tim và giọng nói của mami.";
                tipStr = "Bắt đầu bôi kem chống rạn da và bổ sung Sắt, Canxi theo chỉ định.";
            }
            else if (week >= 21 && week <= 28)
            {
                sizeStr = "bằng quả cà tím lớn 🍆";
                devStr = "Bé đã mở mắt, các phế nang phổi đang phát triển để chuẩn bị thở.";
                tipStr = "Làm xét nghiệm dung nạp đường huyết thai kỳ (tuần 24-28).";
            }
            else if (week >= 29 && week <= 36)
            {
                sizeStr = "bằng quả dưa hấu lớn 🍉";
                devStr = "Bé đã quay đầu xuống dưới chuẩn bị cho tư thế sinh thuận.";
                tipStr = "Theo dõi cử động thai (đếm cơn thai máy) ít nhất 3 lần mỗi ngày.";
            }
            else if (week > 36)
            {
                sizeStr = "bằng quả bí đỏ chín muồi 🎃";
                devStr = "Các cơ quan hoàn thiện toàn diện, bé sẵn sàng chào đời bất cứ lúc nào.";
                tipStr = "Chuẩn bị giỏ đồ đi sinh, giấy tờ tùy thân và giữ tinh thần thoải mái.";
            }

            return new
            {
                Week = week,
                BabySize = sizeStr,
                Development = devStr,
                MomTips = tipStr
            };
        }
    }
}
