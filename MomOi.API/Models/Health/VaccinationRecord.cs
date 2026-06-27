using System;

namespace MomOi.API.Models.Health
{
    public enum VaccinationStatus
    {
        Upcoming,
        Completed,
        Overdue,
        Skipped
    }

    public class VaccinationRecord : BaseEntity
    {
        public int BabyProfileId { get; set; }
        public BabyProfile BabyProfile { get; set; } = null!;
        
        public string VaccineName { get; set; } = string.Empty;
        public int RecommendedAgeMonths { get; set; }
        public int DoseNumber { get; set; }
        public DateTime? AdministeredDate { get; set; }
        public VaccinationStatus Status { get; set; } = VaccinationStatus.Upcoming;
        public string? Notes { get; set; }
        public string? ClinicName { get; set; }
    }
}
