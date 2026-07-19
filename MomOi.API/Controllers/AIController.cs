using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.AIFeatures;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// General AI features controller (e.g. generating multiple recipes).
    /// Migrated from Node.js aiController.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/ai")]
    public class AIController : ControllerBase
    {
        private readonly IAIFeatureService _aiFeatureService;

        public AIController(IAIFeatureService aiFeatureService)
        {
            _aiFeatureService = aiFeatureService;
        }

        /// <summary>
        /// Generates multiple AI recipes based on a query.
        /// </summary>
        [HttpPost("recipes")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateAIRecipes([FromBody] GenerateAiRecipesRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _aiFeatureService.GenerateAIRecipesAsync(userId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
