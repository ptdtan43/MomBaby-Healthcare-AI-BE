using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.BusinessRules;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Lifestyle
{
    public class LifestyleService : ILifestyleService
    {
        private readonly IGenericRepository<LifestyleEntry> _entryRepo;
        private readonly IGenericRepository<LifestyleAlert> _alertRepo;
        private readonly IGenericRepository<MomHealthProfile> _profileRepo;
        private readonly IBusinessRuleEngine _ruleEngine;

        public LifestyleService(
            IGenericRepository<LifestyleEntry> entryRepo,
            IGenericRepository<LifestyleAlert> alertRepo,
            IGenericRepository<MomHealthProfile> profileRepo,
            IBusinessRuleEngine ruleEngine)
        {
            _entryRepo = entryRepo;
            _alertRepo = alertRepo;
            _profileRepo = profileRepo;
            _ruleEngine = ruleEngine;
        }

        public async Task<ApiResponse<object>> SubmitLifestyleEntryAsync(string userId, LifestyleEntryRequestDto request)
        {
            var validStress = new[] { "Low", "Moderate", "High" };
            if (!validStress.Contains(request.StressLevel))
                return ApiResponse<object>.FailureResult("Mức độ căng thẳng phải là: 'Low', 'Moderate', hoặc 'High'.");

            var today = DateTime.UtcNow.Date;

            var existing = await _entryRepo.FirstOrDefaultAsync(e => e.UserId == userId && e.Date == today);

            if (existing != null)
            {
                existing.SelfCareHours = request.SelfCareHours;
                existing.SleepHours = request.SleepHours;
                existing.PhysicalHours = request.PhysicalHours;
                existing.SocialHours = request.SocialHours;
                existing.WaterLiters = request.WaterLiters;

                if (Enum.TryParse<StressLevel>(request.StressLevel, true, out var stress))
                {
                    existing.StressLevel = stress;
                }

                existing.HealthScore = ComputeHealthScore(request);
                existing.LifestyleProfile = ClassifyProfile(request);
                existing.UpdatedAt = DateTime.UtcNow;
                _entryRepo.Update(existing);
            }
            else
            {
                var entry = new LifestyleEntry
                {
                    UserId = userId,
                    Date = today,
                    SelfCareHours = request.SelfCareHours,
                    SleepHours = request.SleepHours,
                    PhysicalHours = request.PhysicalHours,
                    SocialHours = request.SocialHours,
                    WaterLiters = request.WaterLiters,
                    HealthScore = ComputeHealthScore(request),
                    LifestyleProfile = ClassifyProfile(request)
                };

                if (Enum.TryParse<StressLevel>(request.StressLevel, true, out var stress))
                {
                    entry.StressLevel = stress;
                }
                entry.CreatedAt = DateTime.UtcNow;
                entry.UpdatedAt = DateTime.UtcNow;
                
                await _entryRepo.AddAsync(entry);
                existing = entry;
            }

            await _entryRepo.SaveChangesAsync();

            try
            {
                var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    var rules = await _ruleEngine.EvaluateAsync(profile);
                    var oldAlerts = await _alertRepo.FindAsync(a => a.UserId == userId);
                    _alertRepo.RemoveRange(oldAlerts);

                    foreach (var rule in rules)
                    {
                        await _alertRepo.AddAsync(new LifestyleAlert
                        {
                            UserId = userId,
                            RuleId = rule.RuleId,
                            Severity = AlertSeverity.Medium,
                            Title = rule.TitleVi,
                            Message = rule.MessageVi,
                            Suggestion = rule.SuggestionVi,
                            TriggeredAt = DateTime.UtcNow,
                            Status = AlertStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    await _alertRepo.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Lifestyle] Rule evaluation error: {ex.Message}");
            }

            return ApiResponse<object>.SuccessResult(existing, "Ghi nhận lối sống hàng ngày thành công.");
        }

        public async Task<ApiResponse<object>> GetTodayEntryAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            var entry = await _entryRepo.FirstOrDefaultAsync(e => e.UserId == userId && e.Date == today);

            if (entry == null)
            {
                return ApiResponse<object>.SuccessResult(new { }, "Chưa có nhật ký cho hôm nay.");
            }

            return ApiResponse<object>.SuccessResult(entry, "Lấy nhật ký hôm nay thành công.");
        }

        public async Task<ApiResponse<object>> GetHistoryAsync(string userId, int days)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var allEntries = await _entryRepo.FindAsync(e => e.UserId == userId && e.Date >= startDate);
            var history = allEntries.OrderBy(e => e.Date).ToList();

            return ApiResponse<object>.SuccessResult(history, "Lấy lịch sử lối sống thành công.");
        }

        public async Task<ApiResponse<object>> GetAlertsAsync(string userId)
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var allAlerts = await _alertRepo.FindAsync(a => a.UserId == userId && a.TriggeredAt >= sevenDaysAgo && a.Severity == AlertSeverity.High);
            var alerts = allAlerts.OrderByDescending(a => a.TriggeredAt).ToList();

            return ApiResponse<object>.SuccessResult(alerts, "Lấy cảnh báo lối sống 7 ngày gần nhất thành công.");
        }

        public async Task<ApiResponse<object>> GetSummaryAsync(string userId)
        {
            var allEntries = await _entryRepo.FindAsync(e => e.UserId == userId);
            var entries = allEntries.OrderByDescending(e => e.Date).ToList();

            if (!entries.Any())
            {
                return ApiResponse<object>.SuccessResult(new
                {
                    avgHealthScore = 0,
                    mostTriggeredRules = Array.Empty<object>(),
                    streakCount = 0
                }, "Chưa có dữ liệu lối sống.");
            }

            var avgHealthScore = (int)Math.Round(entries.Average(e => e.HealthScore));

            int streakCount = 0;
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var latestDate = entries[0].Date;

            if (latestDate == today || latestDate == yesterday)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].HealthScore >= 70)
                    {
                        if (i == 0) { streakCount = 1; }
                        else
                        {
                            var diff = (entries[i - 1].Date - entries[i].Date).Days;
                            if (diff == 1) streakCount++;
                            else break;
                        }
                    }
                    else break;
                }
            }

            var latest = entries[0];
            var prevScore = entries.Count > 1 ? entries[1].HealthScore : latest.HealthScore;

            var radarData = new
            {
                sleep = Math.Min(100, (int)Math.Round(latest.SleepHours / 8 * 100)),
                physical = Math.Min(100, (int)Math.Round(latest.PhysicalHours / 1.5 * 100)),
                selfCare = Math.Min(100, (int)Math.Round(latest.SelfCareHours / 2 * 100)),
                water = Math.Min(100, (int)Math.Round(latest.WaterLiters / 2.5 * 100))
            };

            var scoreTrends = entries
                .Take(30)
                .OrderBy(e => e.Date)
                .Select(e => new
                {
                    date = e.Date.ToString("d/M"),
                    healthScore = e.HealthScore
                });

            return ApiResponse<object>.SuccessResult(new
            {
                healthScore = latest.HealthScore,
                trend = latest.HealthScore - prevScore,
                radarData,
                scoreTrends,
                avgHealthScore,
                streakCount
            }, "Tổng hợp sức khỏe lối sống thành công.");
        }

        #region Private Scoring Logic

        private static int ComputeHealthScore(LifestyleEntryRequestDto r)
        {
            float score = 100;
            if (r.SleepHours < 7) score -= 15;
            if (r.PhysicalHours < 0.5) score -= 10;
            if (r.SelfCareHours < 1) score -= 10;
            if (r.WaterLiters < 2) score -= 10;
            if (r.StressLevel == "High") score -= 20;

            return (int)Math.Max(0, Math.Min(100, score));
        }

        private static MaternalLifestyleProfile ClassifyProfile(LifestyleEntryRequestDto r)
        {
            if (r.StressLevel == "High" && r.SleepHours < 6)
                return MaternalLifestyleProfile.Exhausted;
            if (r.PhysicalHours < 0.5)
                return MaternalLifestyleProfile.Sedentary;
            if (r.PhysicalHours >= 1 && r.SleepHours >= 7 && r.SelfCareHours >= 1)
                return MaternalLifestyleProfile.Active;
            if (r.SleepHours >= 6)
                return MaternalLifestyleProfile.Balanced;

            return MaternalLifestyleProfile.Unknown;
        }

        #endregion
    }
}
