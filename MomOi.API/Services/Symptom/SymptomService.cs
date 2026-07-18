using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using MomOi.API.Services.Integration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SymptomService> _logger;

        public SymptomService(
            IGenericRepository<MomHealthProfile> profileRepo,
            IGenericRepository<SymptomLog> symptomRepo,
            IGenericRepository<NotificationAlert> alertRepo,
            IGeminiService geminiService,
            ILogger<SymptomService> logger)
        {
            _profileRepo = profileRepo;
            _symptomRepo = symptomRepo;
            _alertRepo = alertRepo;
            _geminiService = geminiService;
            _logger = logger;
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
            UrgencyLevel urgencyLevel = UrgencyLevel.Low;
            bool shouldSeeDoctor = false;
            bool aiSucceeded = false;

            try
            {
                aiResponseJson = await _geminiService.GenerateJsonAsync(prompt);

                using var doc = System.Text.Json.JsonDocument.Parse(aiResponseJson);
                if (doc.RootElement.TryGetProperty("severityScore", out var scoreEl))
                    severityScore = scoreEl.GetInt32();
                if (doc.RootElement.TryGetProperty("urgencyLevel", out var urgencyEl))
                    urgencyLevel = MapUrgencyLevel(urgencyEl.GetString());
                if (doc.RootElement.TryGetProperty("shouldSeeDoctor", out var doctorEl))
                    shouldSeeDoctor = doctorEl.GetBoolean();

                aiSucceeded = true;
            }
            catch (System.Text.Json.JsonException ex)
            {
                // The model replied, but not with the JSON structure we asked for.
                _logger.LogError(ex, "Gemini returned a non-JSON payload for symptom analysis. Raw response: {Raw}", aiResponseJson);
                aiResponseJson = string.Empty;
            }
            catch (Exception ex)
            {
                // Missing/invalid API key, network error, quota exceeded, ...
                _logger.LogError(ex, "Symptom AI analysis failed for user {UserId}.", userId);
                aiResponseJson = string.Empty;
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
                GeminiModel = "gemini-2.5-flash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _symptomRepo.AddAsync(log);

            if (severityScore >= 70)
            {
                await _alertRepo.AddAsync(new NotificationAlert
                {
                    UserId = log.UserId,
                    Type = NotificationAlertType.Symptom,
                    Severity = AlertSeverity.Medium,
                    Message = $"Phát hiện triệu chứng nghiêm trọng (điểm {severityScore}/100). Vui lòng liên hệ bác sĩ ngay.",
                    Status = NotificationStatus.Pending,
                    Channels = new[] { "email", "app" },
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _symptomRepo.SaveChangesAsync();
            await _alertRepo.SaveChangesAsync();

            var message = aiSucceeded
                ? "Ghi nhận triệu chứng và phân tích AI thành công."
                : "Đã ghi nhận triệu chứng, nhưng phân tích AI hiện không khả dụng. Vui lòng thử lại sau.";

            return ApiResponse<object>.SuccessResult(log, message);
        }

        /// <summary>
        /// Maps the urgency level returned by Gemini (Vietnamese, as requested in the prompt)
        /// onto the <see cref="UrgencyLevel"/> enum. English values are accepted as well so the
        /// mapping keeps working if the prompt is ever changed.
        /// </summary>
        private static UrgencyLevel MapUrgencyLevel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return UrgencyLevel.Low;

            return value.Trim().ToLowerInvariant() switch
            {
                "thấp" or "low" => UrgencyLevel.Low,
                "trung bình" or "medium" => UrgencyLevel.Medium,
                "cao" or "high" => UrgencyLevel.High,
                "khẩn cấp" or "critical" => UrgencyLevel.Critical,
                _ => UrgencyLevel.Low
            };
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
