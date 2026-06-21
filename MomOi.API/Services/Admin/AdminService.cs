using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>> GetUsersAtRiskAsync()
        {
            var highRiskAlerts = await _context.LifestyleAlerts
                .Where(a => a.Severity == AlertSeverity.High && a.Status == AlertStatus.Pending)
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TriggeredRules = g.ToList(),
                    LatestAlertDate = g.Max(a => a.TriggeredAt)
                })
                .ToListAsync();

            var userIds = highRiskAlerts.Select(a => a.UserId).ToList();
            var latestEntries = await _context.LifestyleEntries
                .Where(e => userIds.Contains(e.UserId))
                .GroupBy(e => e.UserId)
                .Select(g => g.OrderByDescending(e => e.Date).FirstOrDefault())
                .ToListAsync();

            var result = highRiskAlerts.Select(alert => new
            {
                userId = alert.UserId,
                triggeredRules = alert.TriggeredRules.Select(r => new { r.RuleId, r.Title }),
                healthScore = latestEntries.FirstOrDefault(e => e?.UserId == alert.UserId)?.HealthScore ?? 0,
                latestAlertDate = alert.LatestAlertDate
            }).OrderByDescending(r => r.latestAlertDate);

            return ApiResponse<object>.SuccessResult(result, "Lấy danh sách người dùng có nguy cơ cao thành công.");
        }

        public async Task<ApiResponse<object>> GetReportsSummaryAsync()
        {
            var stressGroups = await _context.LifestyleEntries
                .GroupBy(e => e.StressLevel)
                .Select(g => new { StressLevel = g.Key, Count = g.Count() })
                .ToListAsync();

            var stressDist = new
            {
                Low = stressGroups.FirstOrDefault(g => g.StressLevel == "Low")?.Count ?? 0,
                Moderate = stressGroups.FirstOrDefault(g => g.StressLevel == "Moderate")?.Count ?? 0,
                High = stressGroups.FirstOrDefault(g => g.StressLevel == "High")?.Count ?? 0
            };

            var scoreTrend = await _context.LifestyleEntries
                .GroupBy(e => e.Date.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    avgHealthScore = (int)Math.Round(g.Average(e => e.HealthScore))
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            var topRules = await _context.LifestyleAlerts
                .GroupBy(a => a.RuleId)
                .Select(g => new
                {
                    ruleId = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            return ApiResponse<object>.SuccessResult(new
            {
                stressLevelDistribution = stressDist,
                healthScoreTrend = scoreTrend,
                topTriggeredRules = topRules
            }, "Thống kê báo cáo thành công.");
        }
    }
}
