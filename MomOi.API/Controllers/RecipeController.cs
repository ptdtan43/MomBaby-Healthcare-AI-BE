using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Recipe;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for managing nutritional recipes (bookmarking, retrieving).
    /// Migrated from Node.js recipeRoutes.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/recipes")]
    public class RecipeController : ControllerBase
    {
        private readonly IRecipeService _recipeService;

        public RecipeController(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        /// <summary>
        /// Retrieves the list of recipes associated with the user, optionally filtered by IsSaved.
        /// </summary>
        [HttpGet("my")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyRecipes([FromQuery] bool? isSaved, [FromQuery] MomOi.API.Models.Health.RecipeCategory? category, [FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _recipeService.GetMyRecipesAsync(userId, isSaved, category, page, limit);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Fetches single recipe details by ID.
        /// </summary>
        [HttpGet("{recipeId:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecipe(int recipeId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _recipeService.GetRecipeAsync(userId, recipeId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Toggles the bookmark/save status of a recipe.
        /// </summary>
        [HttpPatch("{recipeId:int}/save")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ToggleSaveRecipe(int recipeId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _recipeService.ToggleSaveRecipeAsync(userId, recipeId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Fetches current student/maternal profile classification details.
        /// </summary>
        [HttpGet("profiles/current")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _recipeService.GetCurrentProfileAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
