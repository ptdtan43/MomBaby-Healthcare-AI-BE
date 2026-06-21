using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Services.UserProfile;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for retrieving and updating the user's isolated health profile and subscription details.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/user-profile")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Retrieves the isolated health profile of the current logged-in user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _userProfileService.GetProfileAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Updates health metrics (e.g. BMI, journey stage, medical conditions) for the current user.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfile([FromBody] MomHealthProfile updateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _userProfileService.UpdateProfileAsync(userId, updateDto);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Simulated endpoint to upgrade a user's subscription tier.
        /// </summary>
        [HttpPost("upgrade")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpgradeSubscription([FromQuery] SubscriptionTier tier)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _userProfileService.UpgradeSubscriptionAsync(userId, tier);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}
