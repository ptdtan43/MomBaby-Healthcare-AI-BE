using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Mom
{
    public class CreateDietPlanDto
    {
        public int? WeekNumber { get; set; }

        [Required]
        public string DailyMealsJson { get; set; } = "[]";
    }

    public class GenerateDietPlanDto
    {
        public int? WeekNumber { get; set; }

        [Required]
        public int BabyAgeInMonths { get; set; }

        [Required]
        public float BabyWeightKg { get; set; }

        public string AdditionalNotes { get; set; } = string.Empty;
    }
}
