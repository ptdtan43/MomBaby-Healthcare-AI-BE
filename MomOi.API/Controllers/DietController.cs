using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Diet;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for managing diet plans and AI-generated recipes.
    /// Migrated from Node.js dietController.js + aiDietController.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/diet")]
    public class DietController : ControllerBase
    {
        private readonly IDietService _dietService;

        public DietController(IDietService dietService)
        {
            _dietService = dietService;
        }

        /// <summary>
        /// Generates a default weekly diet plan for the user.
        /// </summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateDietPlan()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dietService.GenerateDietPlanAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves the latest diet plan for the current user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDietPlan()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dietService.GetDietPlanAsync(userId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Updates the daily meals for the user's latest diet plan.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDietPlan([FromBody] UpdateDietPlanRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dietService.UpdateDietPlanAsync(userId, request);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Generates a single personalized diet recipe via Gemini AI and adds it to today's plan.
        /// </summary>
        [HttpPost("ai-generate")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateAiDiet([FromBody] GenerateAiDietRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _dietService.GenerateAiDietAsync(userId, request);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}
