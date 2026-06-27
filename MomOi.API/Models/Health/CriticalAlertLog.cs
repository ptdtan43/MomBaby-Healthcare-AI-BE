using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Severity levels for health alerts.
    /// </summary>
    public enum AlertSeverity
    {
        Critical,
        High,
        Medium,
        Warning,
        Info,
        Positive
    }

    /// <summary>
    /// Logs critical and warning clinical alerts triggered by the business rules engine.
    /// </summary>
    public class CriticalAlertLog : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public string RuleId { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; } = AlertSeverity.Info;
        public string TitleVi { get; set; } = string.Empty;
        public string MessageVi { get; set; } = string.Empty;
        public string SuggestionVi { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;
    }
}
