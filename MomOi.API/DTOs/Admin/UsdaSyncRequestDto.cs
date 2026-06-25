using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Admin
{
    public class UsdaSyncRequestDto
    {
        /// <summary>
        /// Search term to query USDA FoodData Central (e.g. "Apple", "Milk").
        /// </summary>
        [Required]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of items to sync (default: 10).
        /// </summary>
        public int MaxItems { get; set; } = 10;
    }
}
