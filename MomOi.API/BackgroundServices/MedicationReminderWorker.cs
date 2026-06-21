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
    public class MedicationReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MedicationReminderWorker> _logger;

        public MedicationReminderWorker(IServiceProvider serviceProvider, ILogger<MedicationReminderWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MedicationReminderWorker is starting.");

            // Run every minute
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in MedicationReminderWorker.");
                }
            }
        }

        private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

            var now = DateTime.UtcNow;
            var timeNow = now.ToString("HH:mm"); // matching exactly HH:mm
            var todayStr = now.Date;

            // Fetch active schedules
            var activeSchedules = await dbContext.MedicationSchedules
                .Include(m => m.AdherenceLogs)
                .Where(m => m.StartDate <= now && m.EndDate >= now)
                .ToListAsync(stoppingToken);

            foreach (var schedule in activeSchedules)
            {
                if (schedule.Times.Contains(timeNow))
                {
                    // Check if already taken or reminded
                    bool hasRemindedOrTaken = schedule.AdherenceLogs
                        .Any(log => log.Date.Date == todayStr && (log.Status == "taken" || log.Status == "reminded"));

                    if (!hasRemindedOrTaken)
                    {
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == schedule.UserId, stoppingToken);
                        if (user != null)
                        {
                            var message = $"Đã đến giờ uống thuốc: {schedule.MedName} - Liều lượng: {schedule.Dosage}. Hãy uống thuốc đúng giờ nhé mẹ bầu!";
                            
                            // Send Push Notification
                            // In a real app we would have FCM token stored in user model. Assuming logic handles it inside service.
                            // We can also create a NotificationAlert DB entry.
                            var alert = new NotificationAlert
                            {
                                UserId = user.Id,
                                Type = NotificationAlertType.Medication,
                                Severity = 50,
                                Message = message,
                                Channels = new[] { "app" },
                                Status = NotificationStatus.Sent,
                                CreatedAt = now
                            };
                            dbContext.NotificationAlerts.Add(alert);

                            // We log a generic status "reminded" to avoid double notification in same minute
                            schedule.AdherenceLogs.Add(new MedicationAdherenceLog
                            {
                                Date = now,
                                Status = "reminded"
                            });

                            _logger.LogInformation("Sent med reminder for {MedName} to {Email}", schedule.MedName, user.Email);
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
