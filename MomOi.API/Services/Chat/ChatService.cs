using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Services.AI;
using MomOi.API.Services.Integration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;

        public ChatService(AppDbContext context, IGeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        public async Task<ApiResponse<object>> SendMessageAsync(string userId, SendMessageRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return ApiResponse<object>.FailureResult("Tin nhắn không được để trống.");
            }

            var profile = await _context.MomHealthProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            var healthContext = BuildHealthContext(profile);

            var botReply = await _geminiService.SendChatMessageAsync(request.Text, healthContext);

            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == request.SessionId);

            if (session == null)
            {
                session = new ChatSession
                {
                    UserId = userId,
                    SessionId = request.SessionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            _context.ChatMessages.Add(new ChatMessage
            {
                ChatSessionId = session.Id,
                Sender = SenderType.User,
                Text = request.Text,
                Timestamp = DateTime.UtcNow
            });
            _context.ChatMessages.Add(new ChatMessage
            {
                ChatSessionId = session.Id,
                Sender = SenderType.Bot,
                Text = botReply,
                Timestamp = DateTime.UtcNow
            });

            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(new { reply = botReply });
        }

        public async Task<ApiResponse<object>> GetChatHistoryAsync(string userId, string? sessionId, int limit = 50)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId);

            if (session == null)
            {
                return ApiResponse<object>.SuccessResult(new { messages = Array.Empty<object>() });
            }

            var messages = session.Messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(limit)
                .Select(m => new
                {
                    sender = m.Sender,
                    text = m.Text,
                    timestamp = m.Timestamp
                });

            return ApiResponse<object>.SuccessResult(new { messages });
        }

        public async Task<ApiResponse<object>> ClearSessionAsync(string userId, string? sessionId)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId);

            if (session != null)
            {
                _context.ChatMessages.RemoveRange(session.Messages);
                await _context.SaveChangesAsync();
            }

            return ApiResponse<object>.SuccessResult(null!, "Đã xoá lịch sử chat thành công.");
        }

        private static string BuildHealthContext(MomHealthProfile? profile)
        {
            if (profile == null) return "Chưa có thông tin hồ sơ sức khỏe.";

            return $"- Giai đoạn: {profile.Stage}\n" +
                   $"- Tuần thai: {profile.PregnancyWeek?.ToString() ?? "N/A"}\n" +
                   $"- BMI: {profile.Bmi?.ToString("F1") ?? "N/A"}\n" +
                   $"- Đường huyết: {profile.BloodSugarLevel?.ToString() ?? "N/A"}\n" +
                   $"- Tiểu đường thai kỳ: {(profile.HasGestDiabetes ? "Có" : "Không")}\n" +
                   $"- Đang cho con bú: {(profile.IsBreastfeeding ? "Có" : "Không")}\n" +
                   $"- Bệnh lý: {(profile.MedicalConditions != null ? string.Join(", ", profile.MedicalConditions) : "Không có")}";
        }
    }
}
