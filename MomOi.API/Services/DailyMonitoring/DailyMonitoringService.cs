using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.BusinessRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.DailyMonitoring
{
    public class DailyMonitoringService : IDailyMonitoringService
    {
        private readonly IGenericRepository<MomHealthProfile> _profileRepo;
        private readonly IGenericRepository<DailyMonitoringLog> _logRepo;
        private readonly IGenericRepository<LifestyleAlert> _alertRepo;
        private readonly IBusinessRuleEngine _ruleEngine;

        public DailyMonitoringService(
            IGenericRepository<MomHealthProfile> profileRepo,
            IGenericRepository<DailyMonitoringLog> logRepo,
            IGenericRepository<LifestyleAlert> alertRepo,
            IBusinessRuleEngine ruleEngine)
        {
            _profileRepo = profileRepo;
            _logRepo = logRepo;
            _alertRepo = alertRepo;
            _ruleEngine = ruleEngine;
        }

        public async Task<ApiResponse<object>> CreateOrUpdateDailyMonitoringAsync(string userId, DailyMonitoringRequestDto request)
        {
            var today = DateTime.UtcNow.Date;
            var logDate = request.Date.HasValue ? request.Date.Value.Date : today;

            var existing = await _logRepo.FirstOrDefaultAsync(d => d.UserId == userId && d.Date == logDate);

            if (existing != null)
            {
                if (request.SleepHours.HasValue) existing.SleepHours = request.SleepHours;
                if (request.SleepQuality.HasValue) existing.SleepQuality = request.SleepQuality;
                if (request.WaterLiters.HasValue) existing.WaterLiters = request.WaterLiters;
                if (request.HadBreakfast.HasValue) existing.HadBreakfast = request.HadBreakfast.Value;
                if (request.HadLunch.HasValue) existing.HadLunch = request.HadLunch.Value;
                if (request.HadDinner.HasValue) existing.HadDinner = request.HadDinner.Value;
                if (request.MoodScore.HasValue) existing.MoodScore = request.MoodScore;
                if (request.MoodNote != null) existing.MoodNote = request.MoodNote;
                if (request.BloodSugar.HasValue) existing.BloodSugar = request.BloodSugar;
                if (request.BloodPressureHigh.HasValue) existing.BloodPressureHigh = request.BloodPressureHigh;
                if (request.BloodPressureLow.HasValue) existing.BloodPressureLow = request.BloodPressureLow;
                if (request.Weight.HasValue) existing.Weight = request.Weight;
                if (request.SymptomSeverity.HasValue) existing.SymptomSeverity = request.SymptomSeverity;
                if (request.SymptomNote != null) existing.SymptomNote = request.SymptomNote;
                if (request.Steps.HasValue) existing.Steps = request.Steps.Value;
                if (request.BabyIronInput.HasValue) existing.BabyIronInput = request.BabyIronInput.Value;
                if (request.BabyFoodTexture != null) existing.BabyFoodTexture = request.BabyFoodTexture;
                if (request.BabyFishServings.HasValue) existing.BabyFishServings = request.BabyFishServings.Value;
                if (request.EpdsScore.HasValue) existing.EpdsScore = request.EpdsScore.Value;
                if (request.ConceptionDayOfCycle.HasValue) existing.ConceptionDayOfCycle = request.ConceptionDayOfCycle.Value;
                if (request.AllergySymptomLogged.HasValue) existing.AllergySymptomLogged = request.AllergySymptomLogged.Value;
                if (request.NewFoodLogged != null) existing.NewFoodLogged = request.NewFoodLogged;
                existing.UpdatedAt = DateTime.UtcNow;
                _logRepo.Update(existing);
            }
            else
            {
                existing = new DailyMonitoringLog
                {
                    UserId = userId,
                    Date = logDate,
                    SleepHours = request.SleepHours,
                    SleepQuality = request.SleepQuality,
                    WaterLiters = request.WaterLiters,
                    HadBreakfast = request.HadBreakfast ?? false,
                    HadLunch = request.HadLunch ?? false,
                    HadDinner = request.HadDinner ?? false,
                    MoodScore = request.MoodScore,
                    MoodNote = request.MoodNote,
                    BloodSugar = request.BloodSugar,
                    BloodPressureHigh = request.BloodPressureHigh,
                    BloodPressureLow = request.BloodPressureLow,
                    Weight = request.Weight,
                    SymptomSeverity = request.SymptomSeverity,
                    SymptomNote = request.SymptomNote,
                    Steps = request.Steps ?? 0,
                    BabyIronInput = request.BabyIronInput ?? 0,
                    BabyFoodTexture = request.BabyFoodTexture ?? string.Empty,
                    BabyFishServings = request.BabyFishServings ?? 0,
                    EpdsScore = request.EpdsScore ?? 0,
                    ConceptionDayOfCycle = request.ConceptionDayOfCycle ?? 0,
                    AllergySymptomLogged = request.AllergySymptomLogged ?? false,
                    NewFoodLogged = request.NewFoodLogged ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _logRepo.AddAsync(existing);
            }

            await _logRepo.SaveChangesAsync();

            try
            {
                var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile != null)
                {
                    var triggeredRules = await _ruleEngine.EvaluateAsync(profile);

                    var allAlerts = await _alertRepo.FindAsync(a => a.UserId == userId);
                    _alertRepo.RemoveRange(allAlerts);

                    foreach (var rule in triggeredRules)
                    {
                        var alert = new LifestyleAlert
                        {
                            UserId = userId,
                            DailyMonitoringLogId = existing.Id,
                            RuleId = rule.RuleId,
                            Severity = AlertSeverity.Medium,
                            Title = rule.TitleVi,
                            Message = rule.MessageVi,
                            Suggestion = rule.SuggestionVi,
                            TriggeredAt = DateTime.UtcNow,
                            Status = AlertStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await _alertRepo.AddAsync(alert);
                    }

                    await _alertRepo.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DailyMonitoring] Rule evaluation error: {ex.Message}");
            }

            return ApiResponse<object>.SuccessResult(existing, "Ghi chép nhật ký sức khỏe hàng ngày thành công! AI đang phân tích...");
        }

        public async Task<ApiResponse<object>> GetTodayMonitoringAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            var entry = await _logRepo.FirstOrDefaultAsync(d => d.UserId == userId && d.Date == today);

            return ApiResponse<object>.SuccessResult(new
            {
                data = entry,
                hasSubmittedToday = entry != null
            }, entry != null ? "Đã tìm thấy nhật ký hôm nay." : "Chưa có nhật ký cho hôm nay.");
        }

        public async Task<ApiResponse<object>> GetHistoryAsync(string userId, int limit)
        {
            var allLogs = await _logRepo.FindAsync(d => d.UserId == userId);
            var history = allLogs.OrderByDescending(d => d.Date).Take(limit).ToList();

            return ApiResponse<object>.SuccessResult(history, "Lấy lịch sử nhật ký thành công.");
        }

        public async Task<ApiResponse<object>> GetInsightsAsync(string userId, int days)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var allLogs = await _logRepo.FindAsync(d => d.UserId == userId && d.Date >= startDate);
            var history = allLogs.OrderByDescending(d => d.Date).ToList();

            if (!history.Any())
            {
                return ApiResponse<object>.SuccessResult(new { insights = (object?)null }, "Chưa có dữ liệu theo dõi.");
            }

            var withSleep = history.Where(h => h.SleepHours.HasValue).ToList();
            var withWater = history.Where(h => h.WaterLiters.HasValue).ToList();
            var withMood = history.Where(h => h.MoodScore.HasValue).ToList();
            var withBp = history.Where(h => h.BloodPressureHigh.HasValue).ToList();
            var withSugar = history.Where(h => h.BloodSugar.HasValue).ToList();
            var withWeight = history.Where(h => h.Weight.HasValue).ToList();

            var totalMeals = history.Sum(h => (h.HadBreakfast ? 1 : 0) + (h.HadLunch ? 1 : 0) + (h.HadDinner ? 1 : 0));
            var possibleMeals = history.Count * 3;

            var insights = new
            {
                AverageSleepHours = withSleep.Any() ? Math.Round(withSleep.Average(h => h.SleepHours!.Value), 2) : 0,
                AverageSleepQuality = withSleep.Any() ? Math.Round(withSleep.Where(h => h.SleepQuality.HasValue).DefaultIfEmpty().Average(h => h?.SleepQuality ?? 0), 2) : 0,
                AverageWaterLiters = withWater.Any() ? Math.Round(withWater.Average(h => h.WaterLiters!.Value), 2) : 0,
                AverageMoodScore = withMood.Any() ? Math.Round(withMood.Average(h => h.MoodScore!.Value), 2) : 0,
                MealConsistencyPercent = possibleMeals > 0 ? Math.Round((double)totalMeals / possibleMeals * 100, 1) : 0,
                VitalsTrends = new
                {
                    AvgBloodSugar = withSugar.Any() ? Math.Round(withSugar.Average(h => h.BloodSugar!.Value), 2) : 0,
                    AvgBloodPressureHigh = withBp.Any() ? Math.Round(withBp.Average(h => h.BloodPressureHigh!.Value), 1) : 0,
                    AvgBloodPressureLow = withBp.Any() ? Math.Round(withBp.Average(h => h.BloodPressureLow!.Value), 1) : 0,
                    AvgWeight = withWeight.Any() ? Math.Round(withWeight.Average(h => h.Weight!.Value), 2) : 0
                },
                TotalDays = history.Count,
                Period = $"{days} ngày"
            };

            return ApiResponse<object>.SuccessResult(new { insights }, "Phân tích xu hướng sức khỏe thành công.");
        }
    }
}
