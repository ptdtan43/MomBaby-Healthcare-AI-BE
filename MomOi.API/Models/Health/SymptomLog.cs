using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// AI-powered symptom analysis record with diagnostic suggestions.
    /// Merges MongoDB SymptomEntry and SymptomAnalysis schemas into one relational entity.
    /// </summary>
    public class SymptomLog : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        /// <summary>User's text description of their symptoms.</summary>
        public string TextDescription { get; set; } = string.Empty;

        /// <summary>Uploaded image URLs stored as PostgreSQL text array.</summary>
        public string[] Images { get; set; } = Array.Empty<string>();

        /// <summary>Pregnancy stage context at time of logging (e.g. "pregnant").</summary>
        public string? ProfileStage { get; set; }

        /// <summary>URL of the primary uploaded image for AI analysis.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>"image/jpeg", "image/png", "image/webp", etc.</summary>
        public string? ImageMimeType { get; set; }

        // --- AI Analysis Result (flattened from nested AnalysisResultSchema) ---

        /// <summary>Serialized JSON of possible conditions list from Gemini analysis.</summary>
        public string? PossibleConditionsJson { get; set; }

        public string? LifestyleConnection { get; set; }

        /// <summary>Low, Medium, High, Critical</summary>
        public UrgencyLevel? UrgencyLevel { get; set; }

        public string? UrgencyReason { get; set; }

        /// <summary>Recommendations as PostgreSQL text array.</summary>
        public string[] Recommendations { get; set; } = Array.Empty<string>();

        /// <summary>Dietary suggestions as PostgreSQL text array.</summary>
        public string[] DietarySuggestions { get; set; } = Array.Empty<string>();

        public string? Disclaimer { get; set; }

        public bool ShouldSeeDoctor { get; set; } = false;

        public string? SpecialistType { get; set; }

        // --- Metadata ---
        public int SeverityScore { get; set; } = 0;
        public bool AlertFlag { get; set; } = false;
        public int? ProcessingTimeMs { get; set; }
        public string GeminiModel { get; set; } = "gemini-2.5-flash";
        public bool IsAdminReviewRequired { get; set; } = false;
    }
}
