using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Middleware;
using MomOi.API.Models.Identity;
using MomOi.API.Services.Fertility;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for managing cycle logs, ovulation predictions, and IVF timelines.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/fertility")]
    public class FertilityController : ControllerBase
    {
        private readonly IFertilityService _fertilityService;

        public FertilityController(IFertilityService fertilityService)
        {
            _fertilityService = fertilityService;
        }

        public class CycleLogRequest
        {
            public DateTime PeriodStartDate { get; set; }
            public int CycleLength { get; set; } = 28;
            public string[] Symptoms { get; set; } = Array.Empty<string>();
        }

        /// <summary>
        /// Logs a cycle entry, updates user profile baseline parameters, and predicts the next ovulation cycle.
        /// </summary>
        [HttpPost("cycle-log")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LogCycle([FromBody] CycleLogRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _fertilityService.LogCycleAsync(userId, request.PeriodStartDate, request.CycleLength, request.Symptoms);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves the fertility calendar predictions for a given target month (format: YYYY-MM).
        /// </summary>
        [HttpGet("calendar")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCalendar([FromQuery] string month)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _fertilityService.GetCalendarAsync(userId, month);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Checks ovulation and fertile window status for today, triggering BR01 if applicable.
        /// </summary>
        [HttpGet("ovulation-today")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOvulationToday()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _fertilityService.GetOvulationTodayAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        public class IvfTimelineRequest
        {
            public DateTime IvfStartDate { get; set; }
            public string Protocol { get; set; } = "long"; // "long" | "short" | "antagonist"
        }

        /// <summary>
        /// Generates a customized IVF procedure timeline. Gated under the Modern Mom subscription tier.
        /// </summary>
        [HttpPost("ivf-timeline")]
        [RequiresTier(SubscriptionTier.MomHienDai)]
        [ProducesResponseType(typeof(ApiResponse<List<IvfMilestone>>), StatusCodes.Status200OK)]
        public IActionResult CreateIvfTimeline([FromBody] IvfTimelineRequest request)
        {
            var response = _fertilityService.CreateIvfTimeline(request.IvfStartDate, request.Protocol);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
