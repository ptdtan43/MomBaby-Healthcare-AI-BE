using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Chat
{
    public class SendMessageRequestDto
    {
        public string Text { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    public interface IChatService
    {
        Task<ApiResponse<object>> SendMessageAsync(string userId, SendMessageRequestDto request);
        Task<ApiResponse<object>> GetChatHistoryAsync(string userId, string? sessionId, int limit = 50);
        Task<ApiResponse<object>> ClearSessionAsync(string userId, string? sessionId);
    }
}
