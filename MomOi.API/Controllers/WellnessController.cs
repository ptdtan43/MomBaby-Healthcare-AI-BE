using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Wellness;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/wellness")]
    public class WellnessController : ControllerBase
    {
        private readonly IWellnessService _wellnessService;

        public WellnessController(IWellnessService wellnessService)
        {
            _wellnessService = wellnessService;
        }

        [HttpGet("care-calendar")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCareCalendar()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse<object>.FailureResult("Vui lòng đăng nhập lại."));

            return Ok(await _wellnessService.GetCareCalendarAsync(userId));
        }

        [HttpGet("emergency-guide")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GetEmergencyGuide() => Ok(_wellnessService.GetEmergencyGuide());

        [HttpGet("relax-tracks")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GetRelaxTracks() => Ok(_wellnessService.GetRelaxTracks());
    }
}
