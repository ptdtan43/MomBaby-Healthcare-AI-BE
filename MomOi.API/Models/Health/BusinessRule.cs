using System;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Represents a dynamic business rule for health evaluations.
    /// Can be created/updated by Admin to trigger alerts.
    /// </summary>
    public class BusinessRule
    {
        public int Id { get; set; }

        /// <summary>Unique identifier code (e.g., BR01, BR02)</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Title of the rule (e.g., "Thừa cân")</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Detailed description of what this rule does.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The metric being evaluated (e.g., "BMI", "EPDS", "StressLevel", "Weight")
        /// </summary>
        public string TargetMetric { get; set; } = string.Empty;

        /// <summary>
        /// Operator to use for comparison: ">", "<", ">=", "<=", "=="
        /// </summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>
        /// The threshold value to compare against.
        /// </summary>
        public float ThresholdValue { get; set; }

        /// <summary>
        /// The severity of the alert if this rule is triggered.
        /// </summary>
        public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
