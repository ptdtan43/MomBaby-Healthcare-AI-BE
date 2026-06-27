using System;

namespace MomOi.API.Models.Nutrition
{
    /// <summary>
    /// Represents a nutritional food item synced from the USDA FoodData Central API.
    /// Used by experts/AI to fetch accurate nutrition data.
    /// </summary>
    public class UsdaFoodItem : BaseEntity
    {
        /// <summary>FoodData Central ID</summary>
        public int FdcId { get; set; }

        /// <summary>Food name/description from USDA</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Energy/Calories (kcal) per 100g</summary>
        public float Calories { get; set; }

        /// <summary>Protein (g) per 100g</summary>
        public float Protein { get; set; }

        /// <summary>Carbohydrates (g) per 100g</summary>
        public float Carbs { get; set; }

        /// <summary>Total Fat (g) per 100g</summary>
        public float Fat { get; set; }

        public System.DateTime SyncDate { get; set; } = System.DateTime.UtcNow;
    }
}
