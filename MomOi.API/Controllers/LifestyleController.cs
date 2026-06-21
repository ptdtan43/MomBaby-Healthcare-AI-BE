using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Lifestyle;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for lifestyle tracking, health scoring, and wellness alerts.
    /// Migrated from Node.js lifestyleController.js + lifestyleRoutes.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/lifestyle")]
    public class LifestyleController : ControllerBase
    {
        private readonly ILifestyleService _lifestyleService;

        public LifestyleController(ILifestyleService lifestyleService)
        {
            _lifestyleService = lifestyleService;
        }

        /// <summary>
        /// Creates or updates today's lifestyle entry and computes the health score.
        /// Also evaluates business rules to generate wellness alerts.
        /// </summary>
        [HttpPost("entry")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitLifestyleEntry([FromBody] LifestyleEntryRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _lifestyleService.SubmitLifestyleEntryAsync(userId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>Gets today's lifestyle entry.</summary>
        [HttpGet("today")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodayEntry()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _lifestyleService.GetTodayEntryAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>Gets lifestyle history for the last N days (default 30).</summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory([FromQuery] int days = 30)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _lifestyleService.GetHistoryAsync(userId, days);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>Gets HIGH severity lifestyle alerts from the last 7 days.</summary>
        [HttpGet("alerts")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlerts()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _lifestyleService.GetAlertsAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>Returns dashboard summary: avg score, streak count, radar chart data, and most triggered rules.</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _lifestyleService.GetSummaryAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
