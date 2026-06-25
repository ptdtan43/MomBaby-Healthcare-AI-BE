using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Expert
{
    /// <summary>Request to approve or reject a recipe.</summary>
    public class ReviewRecipeDto
    {
        /// <summary>true = Approve, false = Reject.</summary>
        [Required]
        public bool IsApproved { get; set; }

        /// <summary>Optional note from the Expert (required when rejecting).</summary>
        public string? Note { get; set; }
    }

    /// <summary>Request for Expert to send a consultation message to a Mom.</summary>
    public class ConsultDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
