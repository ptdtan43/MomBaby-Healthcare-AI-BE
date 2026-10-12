using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Services.Alert;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for managing notification alerts (email, sms, app).
    /// Migrated from Node.js alertController.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/alerts")]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        /// <summary>
        /// Retrieves all alerts for the current user, optionally filtered by status.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserAlerts([FromQuery] NotificationStatus? status)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _alertService.GetUserAlertsAsync(userId, status);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Creates a manual alert (usually triggered internally, but exposed for flexibility).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateAlertManual([FromBody] CreateAlertRequestDto request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var recipientUserId = !string.IsNullOrWhiteSpace(request.TargetUserId) ? request.TargetUserId : currentUserId;

            var isCareTeam = User.IsInRole(AppRoles.Staff) || User.IsInRole(AppRoles.Expert) || User.IsInRole(AppRoles.Admin);
            if (recipientUserId != currentUserId && !isCareTeam)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<object>.FailureResult("Bạn không có quyền gửi cảnh báo cho người dùng khác."));
            }

            var response = await _alertService.CreateAlertManualAsync(recipientUserId, request);
            return response.Success ? StatusCode(StatusCodes.Status201Created, response) : BadRequest(response);
        }

        /// <summary>
        /// Updates the status of an alert (e.g., mark as resolved or sent).
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateAlertStatus(int id, [FromBody] UpdateAlertStatusRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _alertService.UpdateAlertStatusAsync(userId, id, request);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Deletes an alert.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _alertService.DeleteAlertAsync(userId, id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Marks every pending risk log of a user as resolved (Staff dashboard "đã hỗ trợ").
        /// </summary>
        [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Staff)]
        [HttpPatch("{userId}/resolve")]
        [HttpPost("{userId}/resolve")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResolveAlert(string userId)
        {
            var response = await _alertService.ResolveAlertAsync(userId);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}
