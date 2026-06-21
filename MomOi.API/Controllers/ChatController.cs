using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Chat;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for AI-powered chat with maternal health context and session history.
    /// Migrated from Node.js chatController.js + chatRoutes.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Sends a user message to Gemini AI with maternal health context and saves the session history.
        /// Rate-limited via global Rate Limiter in Program.cs (100 req/min per IP).
        /// </summary>
        [HttpPost("send")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _chatService.SendMessageAsync(userId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves the chat history for the current user's session.
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChatHistory([FromQuery] string? sessionId, [FromQuery] int limit = 50)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _chatService.GetChatHistoryAsync(userId, sessionId, limit);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Deletes all messages in a specific chat session.
        /// </summary>
        [HttpDelete("clear")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearSession([FromQuery] string? sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _chatService.ClearSessionAsync(userId, sessionId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
