using MomOi.API.DTOs;
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
            if (request.Gpa > 4.0f)
                return ApiResponse<object>.FailureResult("GPA không được vượt quá 4.0.");

            var validStress = new[] { "Low", "Moderate", "High" };
            if (!validStress.Contains(request.StressLevel))
                return ApiResponse<object>.FailureResult("Mức độ căng thẳng phải là: 'Low', 'Moderate', hoặc 'High'.");

            var today = DateTime.UtcNow.Date;

            var healthScore = ComputeHealthScore(request);
            var lifestyleProfile = ClassifyProfile(request);

            var existing = await _entryRepo.FirstOrDefaultAsync(e => e.UserId == userId && e.Date == today);

            if (existing != null)
            {
                existing.StudyHours = request.StudyHours;
                existing.SleepHours = request.SleepHours;
                existing.PhysicalHours = request.PhysicalHours;
                existing.SocialHours = request.SocialHours;
                existing.ExtracurricularHours = request.ExtracurricularHours;
                existing.Gpa = request.Gpa;
                existing.StressLevel = request.StressLevel;
                existing.HealthScore = healthScore;
                existing.LifestyleProfile = lifestyleProfile;
                existing.UpdatedAt = DateTime.UtcNow;
                _entryRepo.Update(existing);
            }
            else
            {
                existing = new LifestyleEntry
                {
                    UserId = userId,
                    Date = today,
                    StudyHours = request.StudyHours,
                    SleepHours = request.SleepHours,
                    PhysicalHours = request.PhysicalHours,
                    SocialHours = request.SocialHours,
                    ExtracurricularHours = request.ExtracurricularHours,
                    Gpa = request.Gpa,
                    StressLevel = request.StressLevel,
                    HealthScore = healthScore,
                    LifestyleProfile = lifestyleProfile,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _entryRepo.AddAsync(existing);
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
                study = Math.Min(100, (int)Math.Round(latest.StudyHours / 8 * 100)),
                sleep = Math.Min(100, (int)Math.Round(latest.SleepHours / 8 * 100)),
                physical = Math.Min(100, (int)Math.Round(latest.PhysicalHours / 1.5 * 100)),
                social = Math.Min(100, (int)Math.Round(latest.SocialHours / 3 * 100)),
                gpa = Math.Min(100, (int)Math.Round(latest.Gpa / 4.0 * 100))
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
            double score = 0;
            score += Math.Min(30, r.SleepHours / 8.0 * 30);
            score += Math.Min(25, r.PhysicalHours / 1.5 * 25);
            score += Math.Min(25, r.Gpa / 4.0 * 25);
            score += Math.Min(10, r.SocialHours / 3.0 * 10);
            score += r.StressLevel switch { "Low" => 10, "Moderate" => 5, _ => 0 };
            return (int)Math.Round(Math.Min(100, score));
        }

        private static string ClassifyProfile(LifestyleEntryRequestDto r)
        {
            if (r.SleepHours < 5 || r.StressLevel == "High") return "Burned Out";
            if (r.PhysicalHours < 0.5 && r.SocialHours < 1) return "Couch Scholar";
            if (r.StudyHours > 10 && r.SleepHours < 6) return "Overachiever";
            if (r.SleepHours >= 7 && r.PhysicalHours >= 1 && r.SocialHours >= 1) return "Balanced";
            return "Unknown";
        }

        #endregion
    }
}
