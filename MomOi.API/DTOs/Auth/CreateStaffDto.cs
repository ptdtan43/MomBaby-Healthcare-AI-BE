using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Auth
{
    /// <summary>
    /// Request payload for Admin to create a Staff or Expert account.
    /// </summary>
    public class CreateStaffDto
    {
        /// <summary>Email of the new staff member.</summary>
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Full name of the new staff member.</summary>
        [Required]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Password for the new account.</summary>
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Role to assign: "Staff" or "Expert".</summary>
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
