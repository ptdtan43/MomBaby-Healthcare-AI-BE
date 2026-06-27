using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Nutrition
{
    /// <summary>
    /// Represents an allergen constraint/record for a maternal user.
    /// </summary>
    public class FoodAllergyRecord : BaseEntity
    {
        /// <summary>
        /// Foreign key to AppUser (linked by UserId).
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        /// <summary>
        /// The allergen substance (e.g. "Peanuts", "Shellfish").
        /// </summary>
        public string Allergen { get; set; } = string.Empty;

        /// <summary>
        /// Severity of the allergic reaction.
        /// </summary>
        public AllergySeverity Severity { get; set; }

        /// <summary>
        /// Observed symptoms (e.g. "Hives", "Swelling", "Anaphylaxis").
        /// </summary>
        public string Symptoms { get; set; } = string.Empty;
    }
}
