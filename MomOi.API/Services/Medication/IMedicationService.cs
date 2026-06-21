using MomOi.API.DTOs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MomOi.API.Services.Medication
{
    public class MedicationScheduleRequestDto
    {
        [Required] public string MedName { get; set; } = string.Empty;
        [Required] public string Dosage { get; set; } = string.Empty;
        [Required] public string[] Times { get; set; } = Array.Empty<string>();
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        public string? Notes { get; set; }
    }

    public class AdherenceRequestDto
    {
        [Required] public DateTime Date { get; set; }
        [Required] public string Status { get; set; } = "taken";
    }

    public interface IMedicationService
    {
        Task<ApiResponse<object>> AddMedicationScheduleAsync(string userId, MedicationScheduleRequestDto request);
        Task<ApiResponse<object>> GetUserMedicationsAsync(string userId);
        Task<ApiResponse<object>> UpdateAdherenceAsync(string userId, int id, AdherenceRequestDto request);
        Task<ApiResponse<object>> DeleteMedicationScheduleAsync(string userId, int id);
    }
}
