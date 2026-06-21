using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Symptom
{
    public class SymptomService : ISymptomService
    {
        private readonly IGenericRepository<MomHealthProfile> _profileRepo;
        private readonly IGenericRepository<SymptomLog> _symptomRepo;
        private readonly IGenericRepository<NotificationAlert> _alertRepo;
        private readonly IGeminiService _geminiService;

        public SymptomService(
            IGenericRepository<MomHealthProfile> profileRepo,
            IGenericRepository<SymptomLog> symptomRepo,
            IGenericRepository<NotificationAlert> alertRepo,
            IGeminiService geminiService)
        {
            _profileRepo = profileRepo;
            _symptomRepo = symptomRepo;
            _alertRepo = alertRepo;
            _geminiService = geminiService;
        }

        public async Task<ApiResponse<object>> AddSymptomEntryAsync(string userId, SymptomRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.TextDescription))
            {
                return ApiResponse<object>.FailureResult("Mô tả triệu chứng không được để trống.");
            }

            var startTime = DateTime.UtcNow;

            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            var profileStage = profile?.Stage.ToString() ?? "unknown";

            var prompt = $"Bạn là chuyên gia tư vấn sức khỏe sản khoa. Phân tích các triệu chứng sau của mẹ " +
                         $"đang ở giai đoạn {profileStage}:\n\n\"{request.TextDescription}\"\n\n" +
                         $"Trả về JSON với cấu trúc: " +
                         $"{{\"possibleConditions\":[{{\"name\":\"...\",\"probability\":\"Có thể\",\"description\":\"...\"}}]," +
                         $"\"lifestyleConnection\":\"...\",\"urgencyLevel\":\"Thấp|Trung bình|Cao|Khẩn cấp\"," +
                         $"\"urgencyReason\":\"...\",\"recommendations\":[\"...\"],\"dietarySuggestions\":[\"...\"]," +
                         $"\"disclaimer\":\"...\",\"shouldSeeDoctor\":false,\"specialistType\":\"...\"," +
                         $"\"severityScore\":0}}";

            string aiResponseJson = string.Empty;
            int severityScore = 0;
            string urgencyLevel = "Thấp";
            bool shouldSeeDoctor = false;

            try
            {
                aiResponseJson = await _geminiService.SendChatMessageAsync(prompt, $"Giai đoạn: {profileStage}");
                using var doc = System.Text.Json.JsonDocument.Parse(
                    aiResponseJson.Replace("```json", "").Replace("```", "").Trim());
                if (doc.RootElement.TryGetProperty("severityScore", out var scoreEl))
                    severityScore = scoreEl.GetInt32();
                if (doc.RootElement.TryGetProperty("urgencyLevel", out var urgencyEl))
                    urgencyLevel = urgencyEl.GetString() ?? "Thấp";
                if (doc.RootElement.TryGetProperty("shouldSeeDoctor", out var doctorEl))
                    shouldSeeDoctor = doctorEl.GetBoolean();
            }
            catch
            {
                // AI failure is non-fatal — save entry without analysis
            }

            var processingMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var log = new SymptomLog
            {
                UserId = userId,
                TextDescription = request.TextDescription,
                Images = Array.Empty<string>(),
                ProfileStage = profileStage,
                ImageUrl = request.ImageUrl,
                ImageMimeType = request.ImageMimeType,
                PossibleConditionsJson = aiResponseJson,
                UrgencyLevel = urgencyLevel,
                ShouldSeeDoctor = shouldSeeDoctor,
                SeverityScore = severityScore,
                AlertFlag = severityScore >= 70,
                ProcessingTimeMs = processingMs,
                GeminiModel = "gemini-1.5-flash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _symptomRepo.AddAsync(log);

            if (severityScore >= 70)
            {
                await _alertRepo.AddAsync(new NotificationAlert
                {
                    UserId = userId,
                    Type = NotificationAlertType.Symptom,
                    Severity = severityScore,
                    Message = $"Phát hiện triệu chứng nghiêm trọng (điểm {severityScore}/100). Vui lòng liên hệ bác sĩ ngay.",
                    Status = NotificationStatus.Pending,
                    Channels = new[] { "email", "app" },
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _symptomRepo.SaveChangesAsync();
            await _alertRepo.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(log, "Ghi nhận triệu chứng và phân tích AI thành công.");
        }

        public async Task<ApiResponse<object>> GetSymptomEntriesAsync(string userId, int? minSeverity)
        {
            var allEntries = await _symptomRepo.FindAsync(s => s.UserId == userId);
            var query = allEntries.AsEnumerable();

            if (minSeverity.HasValue)
            {
                query = query.Where(s => s.SeverityScore >= minSeverity.Value);
            }

            var entries = query.OrderByDescending(s => s.CreatedAt).ToList();
            return ApiResponse<object>.SuccessResult(entries);
        }

        public async Task<ApiResponse<object>> GetSymptomEntryByIdAsync(string userId, int id)
        {
            var entry = await _symptomRepo.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (entry == null)
                return ApiResponse<object>.FailureResult("Không tìm thấy bản ghi triệu chứng.");

            return ApiResponse<object>.SuccessResult(entry);
        }
    }
}
