using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Mom;
using MomOi.API.Services.Mom;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    [Route("api/mom")]
    [ApiController]
    [Authorize(Roles = "Mom, Admin")] // Allow Mom and Admin to access these routes
    public class MomController : ControllerBase
    {
        private readonly IMomService _momService;

        public MomController(IMomService momService)
        {
            _momService = momService;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        // ─── Allergies ──────────────────────────────────────────────────────────

        [HttpGet("allergies")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllergies()
        {
            var userId = GetUserId();
            var response = await _momService.GetAllergiesAsync(userId);
            return Ok(response);
        }

        [HttpPost("allergies")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddAllergy([FromBody] CreateAllergyDto dto)
        {
            var userId = GetUserId();
            var response = await _momService.AddAllergyAsync(userId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("allergies/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveAllergy(int id)
        {
            var userId = GetUserId();
            var response = await _momService.RemoveAllergyAsync(userId, id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        // ─── Diet Plans ─────────────────────────────────────────────────────────

        [HttpGet("diet-plans")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDietPlans()
        {
            var userId = GetUserId();
            var response = await _momService.GetDietPlansAsync(userId);
            return Ok(response);
        }

        [HttpPost("diet-plans/manual")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateManualDietPlan([FromBody] CreateDietPlanDto dto)
        {
            var userId = GetUserId();
            var response = await _momService.CreateManualDietPlanAsync(userId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("diet-plans/generate-ai")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateAIDietPlan([FromBody] GenerateDietPlanDto dto)
        {
            var userId = GetUserId();
            var response = await _momService.GenerateAIDietPlanAsync(userId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // ─── Premium Upgrade ────────────────────────────────────────────────────

        [HttpPost("upgrade")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpgradeToPremium([FromBody] UpgradePremiumDto dto)
        {
            var userId = GetUserId();
            var response = await _momService.UpgradeToPremiumAsync(userId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
