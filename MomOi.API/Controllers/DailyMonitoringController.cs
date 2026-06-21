using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.DailyMonitoring;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for daily health monitoring logs: vitals, sleep, meals, mood, and baby metrics.
    /// Migrated from Node.js dailyMonitoringController.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/daily-monitoring")]
    public class DailyMonitoringController : ControllerBase
    {
        private readonly IDailyMonitoringService _dailyMonitoringService;

        public DailyMonitoringController(IDailyMonitoringService dailyMonitoringService)
        {
            _dailyMonitoringService = dailyMonitoringService;
        }

        /// <summary>
        /// Creates or updates today's monitoring log (upsert by userId + date).
        /// Also evaluates Business Rules to generate health alerts.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOrUpdateDailyMonitoring([FromBody] DailyMonitoringRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dailyMonitoringService.CreateOrUpdateDailyMonitoringAsync(userId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Gets today's monitoring entry for the current user.
        /// </summary>
        [HttpGet("today")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodayMonitoring()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dailyMonitoringService.GetTodayMonitoringAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Gets the daily monitoring history for the last N days (default 30).
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory([FromQuery] int limit = 30)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dailyMonitoringService.GetHistoryAsync(userId, limit);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Computes aggregated health insights over the last N days (default 7).
        /// </summary>
        [HttpGet("insights")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInsights([FromQuery] int days = 7)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dailyMonitoringService.GetInsightsAsync(userId, days);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
