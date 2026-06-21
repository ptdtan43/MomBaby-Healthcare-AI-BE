using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>> GetUserReportDataAsync(string userId, int days = 30)
        {
            var userProfile = await _context.MomHealthProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            var since = DateTime.UtcNow.AddDays(-days);

            var entries = await _context.SymptomLogs
                .Where(s => s.UserId == userId && s.CreatedAt >= since)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var alerts = await _context.NotificationAlerts
                .Where(a => a.UserId == userId && a.CreatedAt >= since)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var summary = new
            {
                avgSeverity30 = entries.Any() ? Math.Round(entries.Average(e => e.SeverityScore), 2) : 0,
                maxSeverity = entries.Any() ? entries.Max(e => e.SeverityScore) : 0,
                totalEntries = entries.Count,
                alertCount = alerts.Count
            };

            return ApiResponse<object>.SuccessResult(new
            {
                user = new
                {
                    bmi = userProfile?.Bmi,
                    diseaseTags = userProfile?.MedicalConditions ?? Array.Empty<string>(),
                    stage = userProfile?.Stage.ToString(),
                    pregnancyWeek = userProfile?.PregnancyWeek
                },
                summary,
                entries = entries.Select(e => new
                {
                    id = e.Id,
                    date = e.CreatedAt,
                    description = e.TextDescription,
                    severity = e.SeverityScore,
                    modelResult = e.PossibleConditionsJson
                }),
                alerts
            }, "Dữ liệu báo cáo được tạo thành công.");
        }

        public ApiResponse<object> GenerateReportPDF()
        {
            return ApiResponse<object>.FailureResult("Tính năng xuất PDF đang được phát triển trong phiên bản C#.");
        }
    }
}
