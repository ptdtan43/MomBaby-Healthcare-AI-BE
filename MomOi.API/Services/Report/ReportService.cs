using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<object>> GetUserReportDataAsync(string userId, int days = 30)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var userProfile = await _unitOfWork.Repository<MomHealthProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            var entries = await _unitOfWork.Repository<SymptomLog>()
                .FindAsync(s => s.UserId == userId && s.CreatedAt >= since);
            var entriesSorted = entries.OrderByDescending(s => s.CreatedAt).ToList();

            var alerts = await _unitOfWork.Repository<NotificationAlert>()
                .FindAsync(a => a.UserId == userId && a.CreatedAt >= since);
            var alertsSorted = alerts.OrderByDescending(a => a.CreatedAt).ToList();

            var summary = new
            {
                avgSeverity30 = entriesSorted.Any() ? Math.Round(entriesSorted.Average(e => e.SeverityScore), 2) : 0,
                maxSeverity = entriesSorted.Any() ? entriesSorted.Max(e => e.SeverityScore) : 0,
                totalEntries = entriesSorted.Count,
                alertCount = alertsSorted.Count
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
                entries = entriesSorted.Select(e => new
                {
                    id = e.Id,
                    date = e.CreatedAt,
                    description = e.TextDescription,
                    severity = e.SeverityScore,
                    modelResult = e.PossibleConditionsJson
                }),
                alerts = alertsSorted
            }, "Dữ liệu báo cáo được tạo thành công.");
        }

        public ApiResponse<object> GenerateReportPDF()
        {
            return ApiResponse<object>.FailureResult("Tính năng xuất PDF đang được phát triển trong phiên bản C#.");
        }
    }
}
