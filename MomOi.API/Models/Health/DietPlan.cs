using System;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// A saved or AI-generated diet plan for a user for a specific pregnancy week.
    /// Maps from MongoDB DietPlan schema (with nested dailyMeals flattened to JSON).
    /// </summary>
    public class DietPlan
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int? WeekNumber { get; set; }

        /// <summary>
        /// 7-day meal plan stored as JSON string.
        /// Structure: [{day, meals:[{mealType, recipe, calories, ingredients[], steps[], youtubeLink}]}]
        /// Uses JSONB-compatible string since structure is deeply nested and varies per plan.
        /// </summary>
        public string DailyMealsJson { get; set; } = "[]";

        /// <summary>"manual", "ai-generated", "nutrition-api"</summary>
        public string GeneratedFrom { get; set; } = "manual";

        public DateTime? MonitoringDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
