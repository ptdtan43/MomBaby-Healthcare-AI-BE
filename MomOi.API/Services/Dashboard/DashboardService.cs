using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>> GetUserDashboardAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var todayDate = now.Date;

            // 1. User Profile for BMI
            var profile = await _context.MomHealthProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            double? bmi = profile?.Bmi;

            // 2. Recent Symptoms
            var recentSymptoms = await _context.SymptomLogs
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();

            var severityMetrics = new
            {
                min = recentSymptoms.Any() ? recentSymptoms.Min(s => s.SeverityScore) : 0,
                max = recentSymptoms.Any() ? recentSymptoms.Max(s => s.SeverityScore) : 0,
                avg = recentSymptoms.Any() ? Math.Round(recentSymptoms.Average(s => s.SeverityScore), 2) : 0
            };

            var oneWeekAgo = now.AddDays(-7);
            var weeklySymptoms = await _context.SymptomLogs
                .Where(s => s.UserId == userId && s.CreatedAt >= oneWeekAgo)
                .ToListAsync();

            var weeklySeverity = new
            {
                avg = weeklySymptoms.Any() ? Math.Round(weeklySymptoms.Average(s => s.SeverityScore), 2) : 0,
                count = weeklySymptoms.Count
            };

            // 3. Recent Daily Monitoring
            var recentDailyMonitoring = await _context.DailyMonitoringLogs
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.Date)
                .Take(15)
                .ToListAsync();

            // 4. Alerts (Critical symptoms & Lifestyle)
            var symptomAlerts = await _context.SymptomLogs
                .Where(s => s.UserId == userId && s.AlertFlag)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new { type = "symptom", data = s })
                .ToListAsync();

            var lifestyleAlerts = await _context.LifestyleAlerts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.TriggeredAt)
                .Select(a => new { type = "lifestyle", data = a })
                .ToListAsync();

            var allAlerts = symptomAlerts.Cast<object>().Concat(lifestyleAlerts.Cast<object>()).ToList();
            var alertCount = allAlerts.Count;

            // 5. Medication Schedule for Today
            var meds = await _context.MedicationSchedules
                .Where(m => m.UserId == userId && m.EndDate >= now)
                .Include(m => m.AdherenceLogs)
                .OrderBy(m => m.StartDate)
                .ToListAsync();

            var medicationSchedule = meds.Select(m => new
            {
                _id = m.Id,
                medName = m.MedName,
                dosage = m.Dosage,
                times = m.Times,
                notes = m.Notes,
                status = m.AdherenceLogs.FirstOrDefault(l => l.Date.Date == todayDate)?.Status ?? "pending",
                startDate = m.StartDate,
                endDate = m.EndDate
            });

            // 6. Diet Plan for Today
            var dietPlanEntry = await _context.DietPlans
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            object todayMeals = new object[0];
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
                            todayMeals = JsonSerializer.Deserialize<object>(mealsProp.GetRawText()) ?? new object[0];
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
