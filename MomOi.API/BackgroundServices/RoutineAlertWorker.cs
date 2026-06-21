using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MomOi.API.Data;
using MomOi.API.Models.Health;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MomOi.API.BackgroundServices
{
    public class RoutineAlertWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RoutineAlertWorker> _logger;

        public RoutineAlertWorker(IServiceProvider serviceProvider, ILogger<RoutineAlertWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RoutineAlertWorker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                // Target time: let's say 14:00 UTC (9:00 PM VN)
                var nextRun = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0, DateTimeKind.Utc);
                if (now > nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogInformation("RoutineAlertWorker will run in {DelayHours} hours.", delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                try
                {
                    await ProcessRoutineChecksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in RoutineAlertWorker.");
                }
            }
        }

        private async Task ProcessRoutineChecksAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var users = await dbContext.Users.ToListAsync(stoppingToken);

            foreach (var user in users)
            {
                var alert = new NotificationAlert
                {
                    UserId = user.Id,
                    Type = NotificationAlertType.RoutineCheck,
                    Severity = 50,
                    Message = "Đã đến giờ kiểm tra sức khỏe định kỳ. Hãy dành vài phút cập nhật tình trạng hiện tại của bạn nhé!",
                    Channels = new[] { "app" },
                    Status = NotificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.NotificationAlerts.Add(alert);
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Routine alerts created successfully.");
        }
    }
}
