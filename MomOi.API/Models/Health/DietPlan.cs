using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// A saved or AI-generated diet plan for a user for a specific pregnancy week.
    /// Maps from MongoDB DietPlan schema (with nested dailyMeals flattened to JSON).
    /// </summary>
    public class DietPlan : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public int? WeekNumber { get; set; }

        /// <summary>
        /// 7-day meal plan stored as JSON string.
        /// Structure: [{day, meals:[{mealType, recipe, calories, ingredients[], steps[], youtubeLink}]}]
        /// Uses JSONB-compatible string since structure is deeply nested and varies per plan.
        /// </summary>
        public string DailyMealsJson { get; set; } = "[]";

        /// <summary>Source of generation</summary>
        public DietPlanSource GeneratedFrom { get; set; } = DietPlanSource.Manual;

        public DateTime? MonitoringDate { get; set; }
    }
}
