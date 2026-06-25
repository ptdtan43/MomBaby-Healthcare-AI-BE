using MomOi.API.Models.Health;
using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Admin
{
    public class BusinessRuleDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string TargetMetric { get; set; } = string.Empty;

        [Required]
        public string Operator { get; set; } = string.Empty;

        [Required]
        public float ThresholdValue { get; set; }

        [Required]
        public AlertSeverity Severity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
