using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Medication;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for medication schedules and adherence tracking.
    /// Migrated from Node.js medController.js + medRoutes.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/medications")]
    public class MedicationController : ControllerBase
    {
        private readonly IMedicationService _medicationService;

        public MedicationController(IMedicationService medicationService)
        {
            _medicationService = medicationService;
        }

        /// <summary>
        /// Creates a new medication reminder schedule for the current user.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddMedicationSchedule([FromBody] MedicationScheduleRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _medicationService.AddMedicationScheduleAsync(userId, request);
            return response.Success ? StatusCode(StatusCodes.Status201Created, response) : BadRequest(response);
        }

        /// <summary>
        /// Gets all active (not expired) medication schedules for the current user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserMedications()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _medicationService.GetUserMedicationsAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Records whether a dose was taken or skipped on a specific date.
        /// </summary>
        [HttpPost("{id:int}/adherence")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateAdherence(int id, [FromBody] AdherenceRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _medicationService.UpdateAdherenceAsync(userId, id, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Deletes a medication schedule by ID.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteMedicationSchedule(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _medicationService.DeleteMedicationScheduleAsync(userId, id);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
