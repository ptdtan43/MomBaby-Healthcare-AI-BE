using System;

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
    public class NotificationAlert
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public NotificationAlertType Type { get; set; }

        public int Severity { get; set; }

        public string Message { get; set; } = string.Empty;

        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        /// <summary>Delivery channels (e.g. "email", "sms", "push"). PostgreSQL text array.</summary>
        public string[] Channels { get; set; } = Array.Empty<string>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
