using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MomOi.API.Data;
using MomOi.API.Services.Notifications;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MomOi.API.BackgroundServices
{
    public class LifestyleReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LifestyleReminderWorker> _logger;

        public LifestyleReminderWorker(IServiceProvider serviceProvider, ILogger<LifestyleReminderWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LifestyleReminderWorker is starting.");

            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTime.UtcNow;

                // 21:00 VN is 14:00 UTC
                if (now.Hour == 14)
                {
                    await SendDailySubmissionReminderAsync(stoppingToken);
                }

                // 08:00 VN is 01:00 UTC
                if (now.Hour == 1)
                {
                    await CheckConsecutiveHighAlertsAsync(stoppingToken);
                }
            }
        }

        private async Task SendDailySubmissionReminderAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running Daily Submission Reminder Job...");
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var today = DateTime.UtcNow.Date;
            var users = await dbContext.Users.ToListAsync(stoppingToken);

            foreach (var user in users)
            {
                var entry = await dbContext.LifestyleEntries
                    .FirstOrDefaultAsync(e => e.UserId == user.Id && e.Date.Date == today, stoppingToken);

                if (entry == null && !string.IsNullOrEmpty(user.Email))
                {
                    var subject = "Nhắc nhở hoàn thành nhật ký lối sống hàng ngày";
                    var text = $"Xin chào {user.UserName},\n\nBạn chưa ghi nhận thông tin lối sống ngày hôm nay. Hãy dành ra 1 phút đăng ký thông tin để theo dõi chỉ số sức khỏe của mình nhé!\n\nTrân trọng,\nĐội ngũ MomOi";
                    
                    await emailService.SendEmailAsync(user.Email, subject, text);
                }
            }
        }

        private async Task CheckConsecutiveHighAlertsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running Consecutive High Alerts Check Job...");
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var users = await dbContext.Users.ToListAsync(stoppingToken);

            foreach (var user in users)
            {
                // Fetch the 3 most recent daily entries
                var entries = await dbContext.LifestyleEntries
                    .Where(e => e.UserId == user.Id)
                    .OrderByDescending(e => e.Date)
                    .Take(3)
                    .ToListAsync(stoppingToken);

                if (entries.Count < 3) continue;

                var d0 = entries[0].Date.Date;
                var d1 = entries[1].Date.Date;
                var d2 = entries[2].Date.Date;

                bool isConsecutive = (d0 - d1).TotalDays == 1 && (d1 - d2).TotalDays == 1;

                if (!isConsecutive) continue;

                // Check if they triggered high alerts in those 3 days
                // Since Alerts are decoupled from entries slightly, we check alerts in those dates
                var d0Alerts = await dbContext.LifestyleAlerts.AnyAsync(a => a.UserId == user.Id && a.Severity == MomOi.API.Models.Health.AlertSeverity.High && a.TriggeredAt.Date == d0, stoppingToken);
                var d1Alerts = await dbContext.LifestyleAlerts.AnyAsync(a => a.UserId == user.Id && a.Severity == MomOi.API.Models.Health.AlertSeverity.High && a.TriggeredAt.Date == d1, stoppingToken);
                var d2Alerts = await dbContext.LifestyleAlerts.AnyAsync(a => a.UserId == user.Id && a.Severity == MomOi.API.Models.Health.AlertSeverity.High && a.TriggeredAt.Date == d2, stoppingToken);

                if (d0Alerts && d1Alerts && d2Alerts)
                {
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        var subject = "Cảnh báo: Tình trạng sức khỏe & lối sống đáng báo động";
                        var text = $"Xin chào {user.UserName},\n\nHệ thống ghi nhận bạn đã nhận cảnh báo lối sống mức độ CAO (HIGH) trong 3 ngày liên tiếp. Điều này có thể ảnh hưởng xấu đến sức khỏe thai kỳ của bạn.\n\nVui lòng điều chỉnh lối sống, tăng thời gian ngủ nghỉ và tham khảo ý kiến chuyên gia y tế nếu cần thiết.\n\nThân mến,\nĐội ngũ MomOi";
                        
                        await emailService.SendEmailAsync(user.Email, subject, text);
                    }
                }
            }
        }
    }
}
