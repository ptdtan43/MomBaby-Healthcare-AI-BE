using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Symptom;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for logging maternal symptoms and getting AI-powered analysis.
    /// Migrated from Node.js symptomController.js + symptomRoutes.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/symptoms")]
    public class SymptomController : ControllerBase
    {
        private readonly ISymptomService _symptomService;

        public SymptomController(ISymptomService symptomService)
        {
            _symptomService = symptomService;
        }

        /// <summary>
        /// Logs a symptom entry and runs Gemini AI analysis. Triggers a critical alert if severity >= 70.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddSymptomEntry([FromBody] SymptomRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _symptomService.AddSymptomEntryAsync(userId, request);
            return response.Success ? StatusCode(StatusCodes.Status201Created, response) : BadRequest(response);
        }

        /// <summary>
        /// Gets all symptom entries for the current user, optionally filtered by minimum severity.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSymptomEntries([FromQuery] int? minSeverity)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _symptomService.GetSymptomEntriesAsync(userId, minSeverity);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Gets a specific symptom entry by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSymptomEntryById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _symptomService.GetSymptomEntryByIdAsync(userId, id);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
    }
}
