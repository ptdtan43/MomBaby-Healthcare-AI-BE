using System;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Alert status tracking (AlertSeverity is defined in CriticalAlertLog.cs).
    /// </summary>
    public enum AlertStatus
    {
        Pending,
        Resolved
    }

    /// <summary>
    /// A lifestyle or daily monitoring alert triggered by the Business Rule Engine.
    /// Maps from MongoDB LifestyleAlert schema.
    /// </summary>
    public class LifestyleAlert
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        /// <summary>Optional FK to the DailyMonitoringLog that triggered this alert.</summary>
        public int? DailyMonitoringLogId { get; set; }

        /// <summary>Identifier of the rule that triggered this alert (e.g. "BR01").</summary>
        public string RuleId { get; set; } = string.Empty;

        public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Suggestion { get; set; } = string.Empty;

        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

        public AlertStatus Status { get; set; } = AlertStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A daily lifestyle tracking entry for health scoring and profile classification.
    /// Maps from MongoDB LifestyleEntry schema.
    /// </summary>
    public class LifestyleEntry
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public float StudyHours { get; set; } = 0;
        public float SleepHours { get; set; } = 0;
        public float PhysicalHours { get; set; } = 0;
        public float SocialHours { get; set; } = 0;
        public float ExtracurricularHours { get; set; } = 0;
        public float Gpa { get; set; } = 0;

        /// <summary>"Low", "Moderate", or "High"</summary>
        public string StressLevel { get; set; } = "Low";

        /// <summary>Composite health score from 0 to 100.</summary>
        public int HealthScore { get; set; }

        /// <summary>"Burned Out", "Couch Scholar", "Balanced", "Overachiever", "Unknown"</summary>
        public string LifestyleProfile { get; set; } = "Unknown";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
