using System;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Workflow status for AI-generated recipes awaiting expert review.
    /// </summary>
    public enum RecipeStatus
    {
        /// <summary>AI just generated this recipe, waiting for Expert to review.</summary>
        PendingReview = 0,
        /// <summary>Expert has approved — visible to Moms.</summary>
        Approved = 1,
        /// <summary>Expert rejected — not shown to Moms.</summary>
        Rejected = 2
    }

    /// <summary>
    /// A maternal recipe including ingredients, cooking steps, and nutrition info.
    /// Maps from MongoDB Recipe schema.
    /// Stage-specific for pre-natal, pregnant, or post-natal mothers.
    /// </summary>
    public class Recipe
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        /// <summary>Maternal journey stage: "pre-natal", "pregnant", "post-natal".</summary>
        public string ProfileStage { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Ingredients stored as JSON string (serialized list of {name, amount, unit}).
        /// </summary>
        public string IngredientsJson { get; set; } = "[]";

        /// <summary>
        /// Cooking steps stored as JSON string (serialized list of {stepNumber, instruction, duration}).
        /// </summary>
        public string StepsJson { get; set; } = "[]";

        // --- Nutrition Info (flattened) ---
        public int Calories { get; set; }
        public string Protein { get; set; } = string.Empty;
        public string Carbs { get; set; } = string.Empty;
        public string Fat { get; set; } = string.Empty;
        public string PrepTime { get; set; } = string.Empty;

        /// <summary>"Dễ", "Trung bình", "Khó"</summary>
        public string Difficulty { get; set; } = "Dễ";

        /// <summary>Searchable tags as PostgreSQL text array.</summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsSaved { get; set; } = false;

        // --- Expert Review Workflow ---

        /// <summary>
        /// Review status: PendingReview (AI created) → Approved/Rejected (Expert decides).
        /// Default is PendingReview when AI generates a new recipe.
        /// </summary>
        public RecipeStatus Status { get; set; } = RecipeStatus.PendingReview;

        /// <summary>Expert's note when reviewing (reason for rejection, suggestions, etc.).</summary>
        public string? ExpertNote { get; set; }

        /// <summary>UserId of the Expert who reviewed this recipe.</summary>
        public string? ReviewedByExpertId { get; set; }

        /// <summary>Timestamp when the Expert completed the review.</summary>
        public DateTime? ReviewedAt { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
