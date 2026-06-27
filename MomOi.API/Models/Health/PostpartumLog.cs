using System.ComponentModel.DataAnnotations.Schema;
using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Represents a recovery log recorded by a mother postpartum.
    /// </summary>
    public class PostpartumLog : BaseEntity
    {
        /// <summary>
        /// Foreign key to AppUser (linked by UserId).
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        /// <summary>
        /// Days elapsed since delivery date.
        /// </summary>
        public int DaysPostpartum { get; set; }

        /// <summary>
        /// Bleeding status classification.
        /// </summary>
        public BleedingStatus? BleedingStatus { get; set; }

        /// <summary>
        /// Brief mood summary.
        /// </summary>
        public string? Mood { get; set; }

        /// <summary>
        /// Custom notes or recovery symptoms.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date when log was saved.
        /// </summary>
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
