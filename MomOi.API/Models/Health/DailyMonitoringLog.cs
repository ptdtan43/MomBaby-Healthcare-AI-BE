using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Daily health monitoring log capturing vitals, mood, sleep, meals, and baby metrics.
    /// Maps from MongoDB DailyMonitoring schema.
    /// </summary>
    public class DailyMonitoringLog : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public DateTime Date { get; set; }

        // --- Sleep ---
        public float? SleepHours { get; set; }
        public int? SleepQuality { get; set; } // 1-5 scale

        // --- Water ---
        public float? WaterLiters { get; set; }

        // --- Meals ---
        public bool HadBreakfast { get; set; }
        public bool HadLunch { get; set; }
        public bool HadDinner { get; set; }

        // --- Mood ---
        public int? MoodScore { get; set; }
        public string? MoodNote { get; set; }

        // --- Vitals ---
        public float? BloodSugar { get; set; }
        public int? BloodPressureHigh { get; set; }
        public int? BloodPressureLow { get; set; }
        public float? Weight { get; set; }

        // --- Symptoms ---
        public int? SymptomSeverity { get; set; }
        public string? SymptomNote { get; set; }

        // --- Activity & Maternal Metrics ---
        public int Steps { get; set; } = 0;
        public float BabyIronInput { get; set; } = 0;
        public string BabyFoodTexture { get; set; } = string.Empty;
        public int BabyFishServings { get; set; } = 0;
        public int EpdsScore { get; set; } = 0;
        public int ConceptionDayOfCycle { get; set; } = 0;
        public bool AllergySymptomLogged { get; set; } = false;
        public string NewFoodLogged { get; set; } = string.Empty;
    }
}
