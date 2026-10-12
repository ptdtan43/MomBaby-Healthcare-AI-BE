using MomOi.API.DTOs.Auth;
using System.Threading.Tasks;

namespace MomOi.API.Services.Auth
{
    /// <summary>
    /// Service contract for handling user registration, authentication, token refresh, and session termination.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new AppUser and initializes an empty MomHealthProfile.
        /// </summary>
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

        /// <summary>
        /// Validates user credentials and returns active access and refresh tokens.
        /// </summary>
        Task<AuthResponseDto> LoginAsync(LoginDto dto);

        /// <summary>
        /// Re-issues access and refresh tokens using a valid refresh token.
        /// </summary>
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto);

        /// <summary>
        /// Revokes the given device's refresh token, or every device's when none is given.
        /// </summary>
        Task LogoutAsync(string userId, string? refreshToken);
    }
}
