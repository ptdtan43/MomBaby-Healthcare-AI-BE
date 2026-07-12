using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<object>> GetUserDashboardAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var todayDate = now.Date;

            // 1. User Profile for BMI
            var profile = await _unitOfWork.Repository<MomHealthProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            double? bmi = profile?.Bmi;

            // 2. Recent Symptoms
            var allSymptoms = await _unitOfWork.Repository<SymptomLog>()
                .FindAsync(s => s.UserId == userId);
            var recentSymptoms = allSymptoms.OrderByDescending(s => s.CreatedAt).Take(10).ToList();

            var severityMetrics = new
            {
                min = recentSymptoms.Any() ? recentSymptoms.Min(s => s.SeverityScore) : 0,
                max = recentSymptoms.Any() ? recentSymptoms.Max(s => s.SeverityScore) : 0,
                avg = recentSymptoms.Any() ? Math.Round(recentSymptoms.Average(s => s.SeverityScore), 2) : 0
            };

            var oneWeekAgo = now.AddDays(-7);
            var weeklySymptoms = allSymptoms.Where(s => s.CreatedAt >= oneWeekAgo).ToList();
            var weeklySeverity = new
            {
                avg = weeklySymptoms.Any() ? Math.Round(weeklySymptoms.Average(s => s.SeverityScore), 2) : 0,
                count = weeklySymptoms.Count
            };

            // 3. Recent Daily Monitoring
            var allMonitoring = await _unitOfWork.Repository<DailyMonitoringLog>()
                .FindAsync(d => d.UserId == userId);
            var recentDailyMonitoring = allMonitoring.OrderByDescending(d => d.Date).Take(15).ToList();

            // 4. Alerts
            var symptomAlerts = recentSymptoms
                .Where(s => s.AlertFlag)
                .Select(s => (object)new { type = "symptom", data = s })
                .ToList();

            var allLifestyleAlerts = await _unitOfWork.Repository<LifestyleAlert>()
                .FindAsync(a => a.UserId == userId);
            var lifestyleAlerts = allLifestyleAlerts
                .OrderByDescending(a => a.TriggeredAt)
                .Select(a => (object)new { type = "lifestyle", data = a })
                .ToList();

            var allAlerts = symptomAlerts.Concat(lifestyleAlerts).ToList();
            var alertCount = allAlerts.Count;

            // 5. Medication Schedule for Today
            var meds = await _unitOfWork.Repository<MedicationSchedule>()
                .FindAsync(m => m.UserId == userId && m.EndDate >= now);
            var medsSorted = meds.OrderBy(m => m.StartDate).ToList();

            // Load adherence logs separately
            var allAdherenceLogs = await _unitOfWork.Repository<MedicationAdherenceLog>()
                .FindAsync(l => medsSorted.Select(m => m.Id).Contains(l.MedicationScheduleId));

            var medicationSchedule = medsSorted.Select(m => new
            {
                _id = m.Id,
                medName = m.MedName,
                dosage = m.Dosage,
                times = m.Times,
                notes = m.Notes,
                status = allAdherenceLogs.FirstOrDefault(l => l.MedicationScheduleId == m.Id && l.Date.Date == todayDate)?.Status.ToString().ToLower() ?? "pending",
                startDate = m.StartDate,
                endDate = m.EndDate
            });

            // 6. Diet Plan for Today
            var allDietPlans = await _unitOfWork.Repository<DietPlan>()
                .FindAsync(d => d.UserId == userId);
            var dietPlanEntry = allDietPlans.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

            object todayMeals = Array.Empty<object>();
            string todayName = now.DayOfWeek.ToString();

            if (dietPlanEntry != null && !string.IsNullOrEmpty(dietPlanEntry.DailyMealsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(dietPlanEntry.DailyMealsJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var dayPlan = doc.RootElement.EnumerateArray()
                            .FirstOrDefault(e => e.TryGetProperty("day", out var dayProp) &&
                                                 dayProp.GetString()?.Equals(todayName, StringComparison.OrdinalIgnoreCase) == true);

                        if (dayPlan.ValueKind != JsonValueKind.Undefined && dayPlan.TryGetProperty("meals", out var mealsProp))
                        {
                            todayMeals = JsonSerializer.Deserialize<object>(mealsProp.GetRawText()) ?? Array.Empty<object>();
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors, return empty meals
                }
            }

            var dietPlan = new
            {
                day = todayName,
                meals = todayMeals
            };

            return ApiResponse<object>.SuccessResult(new
            {
                recentSymptoms,
                recentDailyMonitoring,
                alerts = allAlerts,
                medicationSchedule,
                dietPlan,
                severityMetrics,
                weeklySeverity,
                alertCount,
                bmi
            });
        }
    }
}
