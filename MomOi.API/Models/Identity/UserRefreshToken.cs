using System;

namespace MomOi.API.Models.Identity
{
    /// <summary>
    /// One row per signed-in device, so logging in on the web no longer invalidates the
    /// mobile session. Only the SHA-256 hash of the token is stored: a leaked database
    /// dump cannot be replayed as live sessions.
    /// </summary>
    public class UserRefreshToken : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        public AppUser User { get; set; } = null!;

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
