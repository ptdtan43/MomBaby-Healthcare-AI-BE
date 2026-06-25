using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Mom
{
    public class CreateAllergyDto
    {
        [Required]
        public string Allergen { get; set; } = string.Empty;

        [Required]
        public string Severity { get; set; } = string.Empty;

        public string Symptoms { get; set; } = string.Empty;
    }

    public class AllergyResponseDto
    {
        public int Id { get; set; }
        public string Allergen { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty;
    }
}
