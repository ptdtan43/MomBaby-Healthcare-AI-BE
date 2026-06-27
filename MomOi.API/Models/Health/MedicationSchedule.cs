using System.Collections.Generic;
using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// A single medication reminder schedule for a user.
    /// Maps from MongoDB MedSchedule schema (including nested AdherenceLog).
    /// </summary>
    public class MedicationSchedule : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        /// <summary>Name of the medication (e.g. "Axit Folic 400mcg").</summary>
        public string MedName { get; set; } = string.Empty;

        /// <summary>Dosage instruction (e.g. "1 viên/ngày").</summary>
        public string Dosage { get; set; } = string.Empty;

        /// <summary>Reminder times in HH:mm format, stored as PostgreSQL text array.</summary>
        public string[] Times { get; set; } = Array.Empty<string>();

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string? Notes { get; set; }

        // Navigation property to adherence records
        public ICollection<MedicationAdherenceLog> AdherenceLogs { get; set; }
            = new List<MedicationAdherenceLog>();
    }

    /// <summary>
    /// Records whether a medication dose was taken or skipped on a given date.
    /// </summary>
    public class MedicationAdherenceLog : BaseEntity
    {
        public int MedicationScheduleId { get; set; }

        public DateTime Date { get; set; }

        /// <summary>Taken or Skipped</summary>
        public AdherenceStatus Status { get; set; } = AdherenceStatus.Taken;

        // Navigation property
        public MedicationSchedule Schedule { get; set; } = null!;
    }
}
