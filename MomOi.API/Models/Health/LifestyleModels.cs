using MomOi.API.Models.Identity;
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
    public class LifestyleAlert : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

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
    }

    /// <summary>
    /// A daily lifestyle tracking entry for health scoring and profile classification.
    /// Maps from MongoDB LifestyleEntry schema.
    /// </summary>
    public class LifestyleEntry : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public DateTime Date { get; set; }

        public float SelfCareHours { get; set; } = 0;
        public float SleepHours { get; set; } = 0;
        public float PhysicalHours { get; set; } = 0;
        public float SocialHours { get; set; } = 0;
        public float WaterLiters { get; set; } = 0;

        public StressLevel StressLevel { get; set; } = StressLevel.Low;

        /// <summary>Composite health score from 0 to 100.</summary>
        public int HealthScore { get; set; }

        public MaternalLifestyleProfile LifestyleProfile { get; set; } = MaternalLifestyleProfile.Unknown;
    }
}
