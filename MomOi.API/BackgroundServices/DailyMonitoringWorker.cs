using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MomOi.API.Data;
using MomOi.API.Models.Health;
using MomOi.API.Services.Notifications;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MomOi.API.BackgroundServices
{
    public class DailyMonitoringWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyMonitoringWorker> _logger;

        public DailyMonitoringWorker(IServiceProvider serviceProvider, ILogger<DailyMonitoringWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyMonitoringWorker is starting.");

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTime.UtcNow;
                
                // UTC equivalents: 7:00 VN is 00:00 UTC. 20:00 VN is 13:00 UTC.
                bool isMorning = now.Hour == 0 && now.Minute < 30;
                bool isEvening = now.Hour == 13 && now.Minute < 30;

                if (isMorning)
                {
                    await ProcessDailyMonitoringReminderAsync("morning", stoppingToken);
                }
                else if (isEvening)
                {
                    await ProcessDailyMonitoringReminderAsync("evening", stoppingToken);
                }
            }
        }

        private async Task ProcessDailyMonitoringReminderAsync(string timeOfDay, CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var users = await dbContext.Users.ToListAsync(stoppingToken);
            var todayStr = DateTime.UtcNow.Date;

            foreach (var user in users)
            {
                var todayEntry = await dbContext.DailyMonitoringLogs
                    .FirstOrDefaultAsync(l => l.UserId == user.Id && l.Date.Date == todayStr, stoppingToken);

                if (todayEntry == null)
                {
                    var title = timeOfDay == "morning" ? "Chào buổi sáng!" : "Chào buổi tối!";
                    var message = $"Đừng quên cập nhật chỉ số theo dõi hàng ngày (Huyết áp, đường huyết) của bạn vào sáng nay nhé. Việc này giúp theo dõi sát sao sức khỏe thai kỳ.";

                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await emailService.SendEmailAsync(user.Email, "Nhắc nhở cập nhật chỉ số hàng ngày", message);
                    }

                    _logger.LogInformation("Sent {TimeOfDay} daily monitoring reminder to {Email}", timeOfDay, user.Email);
                }
            }
        }
    }
}
