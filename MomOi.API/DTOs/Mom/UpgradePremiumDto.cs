using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Mom
{
    public class UpgradePremiumDto
    {
        [Required]
        public string PaymentMethod { get; set; } = "MoMo"; // Default to MoMo

        [Required]
        public string TransactionId { get; set; } = string.Empty;

        public int MonthsToUpgrade { get; set; } = 1;
    }
}
