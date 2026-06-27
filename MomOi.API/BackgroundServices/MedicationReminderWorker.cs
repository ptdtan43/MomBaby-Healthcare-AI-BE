using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MomOi.API.Data;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Services.Integration;
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
                    // Check if already taken or skipped
                    bool alreadyLogged = schedule.AdherenceLogs
                        .Any(a => a.Date.Date == todayStr && (a.Status == AdherenceStatus.Taken || a.Status == AdherenceStatus.Skipped));

                    if (!alreadyLogged)
                    {
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == schedule.UserId, stoppingToken);
                        if (user != null)
                        {
                            // Send Push Notification
                            // In a real app we would have FCM token stored in user model. Assuming logic handles it inside service.
                            // We can also create a NotificationAlert DB entry.
                            var alert = new NotificationAlert
                            {
                                UserId = user.Id,
                                Type = NotificationAlertType.Medication,
                                Severity = AlertSeverity.Info,
                                Message = $"Đã đến giờ uống thuốc: {schedule.MedName} ({schedule.Dosage}). Nhớ đánh dấu sau khi uống nhé!",
                                Status = NotificationStatus.Pending,
                                CreatedAt = now
                            };
                            dbContext.NotificationAlerts.Add(alert);

                            // We log a generic status "reminded" to avoid double notification in same minute
                            var reminderLog = new MedicationAdherenceLog
                            {
                                MedicationScheduleId = schedule.Id,
                                Date = todayStr,
                                Status = AdherenceStatus.Reminded
                            };
                            dbContext.MedicationAdherenceLogs.Add(reminderLog);

                            _logger.LogInformation("Sent med reminder for {MedName} to {Email}", schedule.MedName, user.Email);
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
