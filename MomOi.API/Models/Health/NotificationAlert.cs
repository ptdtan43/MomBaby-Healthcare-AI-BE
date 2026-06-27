using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Type of notification alert.
    /// </summary>
    public enum NotificationAlertType
    {
        Symptom,
        Medication,
        RoutineCheck
    }

    /// <summary>
    /// Notification alert status.
    /// </summary>
    public enum NotificationStatus
    {
        Pending,
        Sent,
        Resolved
    }

    /// <summary>
    /// A notification/alert record for symptom, medication, or routine check reminders.
    /// Maps from MongoDB Alert schema.
    /// </summary>
    public class NotificationAlert : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public NotificationAlertType Type { get; set; }

        public AlertSeverity Severity { get; set; }

        public string Message { get; set; } = string.Empty;

        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        public string[] Channels { get; set; } = Array.Empty<string>();
    }
}
