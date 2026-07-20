using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Services.Baby;
using MomOi.API.Services.BusinessRules;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for managing baby profiles and tracking growth milestones.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/baby")]
    public class BabyController : ControllerBase
    {
        private readonly IBabyService _babyService;

        public BabyController(IBabyService babyService)
        {
            _babyService = babyService;
        }

        /// <summary>
        /// Creates a new profile for a baby.
        /// </summary>
        [HttpPost("profile")]
        [ProducesResponseType(typeof(ApiResponse<BabyProfile>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateBabyProfile([FromBody] BabyProfile profile)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _babyService.CreateBabyProfileAsync(userId, profile);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves all baby profiles linked to the current user.
        /// </summary>
        [HttpGet("profiles")]
        [ProducesResponseType(typeof(ApiResponse<List<BabyProfile>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBabyProfiles()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _babyService.GetBabyProfilesAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Gets today's personalized menu for a baby (allergens excluded, WHO goals applied).
        /// Proxied to the Python nutrition recommendation engine.
        /// </summary>
        [HttpGet("{id}/menu/daily")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDailyMenu(int id, [FromQuery] bool force = false)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _babyService.GetBabyMenuAsync(userId, id, weekly: false, forceRefresh: force);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Gets a 7-day personalized menu for a baby (allergens excluded, WHO goals applied).
        /// </summary>
        [HttpGet("{id}/menu/weekly")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWeeklyMenu(int id, [FromQuery] bool force = false)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _babyService.GetBabyMenuAsync(userId, id, weekly: true, forceRefresh: force);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Logs a growth milestone (weight/height) for a baby and provides developmental feedback.
        /// </summary>
        [HttpPost("{id}/growth")]
        [ProducesResponseType(typeof(ApiResponse<GrowthEvaluationResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LogGrowth(int id, [FromBody] GrowthRecord record)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _babyService.LogGrowthAsync(userId, id, record);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Deletes a specific growth milestone record.
        /// </summary>
        [HttpDelete("{id}/growth/{recordId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteGrowth(int id, int recordId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _babyService.DeleteGrowthRecordAsync(userId, id, recordId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Updates an existing baby profile.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<BabyProfile>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateBabyProfile(int id, [FromBody] BabyProfile profile)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _babyService.UpdateBabyProfileAsync(userId, id, profile);
            return response.Success ? Ok(response) : BadRequest(response);
        }

    }
}
