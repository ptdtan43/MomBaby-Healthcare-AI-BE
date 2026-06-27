using System;
using System.Collections.Generic;
using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Nutrition
{
    /// <summary>
    /// Represents a meal logged by a user for nutritional tracking.
    /// </summary>
    public class MealLog : BaseEntity
    {
        /// <summary>
        /// Foreign key to AppUser (linked by UserId).
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        /// <summary>
        /// Time when the meal was logged.
        /// </summary>
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Type of meal.
        /// </summary>
        public MealType MealType { get; set; }

        /// <summary>
        /// List of foods consumed, stored as a JSON string list in the database.
        /// </summary>
        public string[] FoodItems { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Total energy content in kilocalories (kcal).
        /// </summary>
        public float Calories { get; set; }

        /// <summary>
        /// Total carbohydrates in grams.
        /// </summary>
        public float Carbs { get; set; }

        /// <summary>
        /// Total protein in grams.
        /// </summary>
        public float Protein { get; set; }

        /// <summary>
        /// Total fat in grams.
        /// </summary>
        public float Fat { get; set; }
    }
}
