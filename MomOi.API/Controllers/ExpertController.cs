using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Expert;
using MomOi.API.Models.Identity;
using MomOi.API.Services.Expert;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for Expert (Medical Expert / Nutritionist) operations.
    /// Experts review AI-generated recipes and consult assigned Moms.
    /// </summary>
    [Authorize(Roles = AppRoles.Expert)]
    [ApiController]
    [Route("api/expert")]
    public class ExpertController : ControllerBase
    {
        private readonly IExpertService _expertService;

        public ExpertController(IExpertService expertService)
        {
            _expertService = expertService;
        }

        // ─── Recipe Review ───────────────────────────────────────────────────────

        /// <summary>
        /// Get all AI-generated recipes waiting for Expert review (Status = PendingReview).
        /// </summary>
        [HttpGet("recipes/pending")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingRecipes()
        {
            var response = await _expertService.GetPendingRecipesAsync();
            return Ok(response);
        }

        /// <summary>
        /// Approve or reject a recipe. Provide IsApproved=true to approve, false to reject with a Note.
        /// </summary>
        [HttpPatch("recipes/{recipeId:int}/review")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReviewRecipe(int recipeId, [FromBody] ReviewRecipeDto dto)
        {
            var expertId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(expertId)) return Unauthorized();

            var response = await _expertService.ReviewRecipeAsync(recipeId, expertId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // ─── Mom Consultation ─────────────────────────────────────────────────────

        /// <summary>
        /// Get the list of Moms available for consultation.
        /// </summary>
        [HttpGet("moms")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignedMoms()
        {
            var response = await _expertService.GetAssignedMomsAsync();
            return Ok(response);
        }

        /// <summary>
        /// Send a consultation message to a specific Mom.
        /// </summary>
        [HttpPost("moms/{momId}/consult")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConsultMom(string momId, [FromBody] ConsultDto dto)
        {
            var expertId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(expertId)) return Unauthorized();

            var response = await _expertService.ConsultMomAsync(momId, expertId, dto);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}
