using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;

using MomOi.API.Services.AI;
using MomOi.API.Services.Postpartum;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for managing postpartum recovery, lactation logs, and EPDS/voice screening.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/postpartum")]
    public class PostpartumController : ControllerBase
    {
        private readonly IPostpartumService _postpartumService;

        public PostpartumController(IPostpartumService postpartumService)
        {
            _postpartumService = postpartumService;
        }

        public class SetupPostpartumRequest
        {
            public DateTime DeliveryDate { get; set; }
            public string DeliveryType { get; set; } = "natural"; // "natural" | "cesarean"
            public bool IsBreastfeeding { get; set; }
        }

        /// <summary>
        /// Registers a postpartum profile setup, computing days postpartum, healing phases, and recovery recommendations.
        /// </summary>
        [HttpPost("setup")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetupPostpartum([FromBody] SetupPostpartumRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _postpartumService.SetupPostpartumAsync(userId, request.DeliveryDate, request.DeliveryType, request.IsBreastfeeding);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        public class EpdsRequest
        {
            public int[] Answers { get; set; } = new int[10];
        }

        /// <summary>
        /// Submits answers to the 10 EPDS questions. Triggers BR05 rule evaluation and returns Gemini generated empathetic messages.
        /// </summary>
        [HttpPost("epds")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitEpds([FromBody] EpdsRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _postpartumService.SubmitEpdsAsync(userId, request.Answers);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        public class VoiceJournalRequest
        {
            public string AudioBase64 { get; set; } = string.Empty;
            public string MimeType { get; set; } = "audio/mp3";
        }

        /// <summary>
        /// Analyzes a spoken audio journal entry using Gemini 1.5 Pro multimodal capabilities.
        /// </summary>
        [HttpPost("epds/voice-journal")]
        [ProducesResponseType(typeof(ApiResponse<VoiceJournalResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AnalyzeVoiceJournal([FromBody] VoiceJournalRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (string.IsNullOrEmpty(request.AudioBase64))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Dữ liệu âm thanh không được để trống."));
            }

            var response = await _postpartumService.AnalyzeVoiceJournalAsync(userId, request.AudioBase64, request.MimeType);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        public class BreastfeedingLogRequest
        {
            public string Side { get; set; } = "both"; // "left" | "right" | "both"
            public int DurationMinutes { get; set; }
            public DateTime Time { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Records breastfeeding session logs and calculates feeding trends.
        /// </summary>
        [HttpPost("breastfeeding-log")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LogBreastfeeding([FromBody] BreastfeedingLogRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _postpartumService.LogBreastfeedingAsync(userId, request.Side, request.DurationMinutes, request.Time);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Generates pelvic floor, walking, and core workout plans based on recovery progress days.
        /// </summary>
        [HttpGet("recovery-plan")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecoveryPlan([FromQuery] int? day)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _postpartumService.GetRecoveryPlanAsync(userId, day);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves the latest EPDS survey result for the current user.
        /// </summary>
        [HttpGet("epds/latest")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatestEpds()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _postpartumService.GetLatestEpdsAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
