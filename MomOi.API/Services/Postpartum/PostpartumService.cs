using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using MomOi.API.Services.BusinessRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Postpartum
{
    public class PostpartumService : IPostpartumService
    {
        private readonly IGenericRepository<MomHealthProfile> _profileRepo;
        private readonly IGenericRepository<EpdsAssessment> _epdsRepo;
        private readonly IGenericRepository<PostpartumLog> _logRepo;
        private readonly IBusinessRuleEngine _businessRuleEngine;
        private readonly IGeminiService _geminiService;

        public PostpartumService(
            IGenericRepository<MomHealthProfile> profileRepo,
            IGenericRepository<EpdsAssessment> epdsRepo,
            IGenericRepository<PostpartumLog> logRepo,
            IBusinessRuleEngine businessRuleEngine,
            IGeminiService geminiService)
        {
            _profileRepo = profileRepo;
            _epdsRepo = epdsRepo;
            _logRepo = logRepo;
            _businessRuleEngine = businessRuleEngine;
            _geminiService = geminiService;
        }

        public async Task<ApiResponse<object>> SetupPostpartumAsync(string userId, DateTime deliveryDate, string deliveryType, bool isBreastfeeding)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new MomHealthProfile
                {
                    UserId = userId,
                    Stage = JourneyStage.Postpartum,
                    DeliveryDate = deliveryDate,
                    IsBreastfeeding = isBreastfeeding,
                    UpdatedAt = DateTime.UtcNow
                };
                await _profileRepo.AddAsync(profile);
            }
            else
            {
                profile.Stage = JourneyStage.Postpartum;
                profile.DeliveryDate = deliveryDate;
                profile.IsBreastfeeding = isBreastfeeding;
                profile.UpdatedAt = DateTime.UtcNow;
                _profileRepo.Update(profile);
            }

            await _profileRepo.SaveChangesAsync();

            var daysPostpartum = (DateTime.UtcNow.Date - deliveryDate.Date).Days;
            if (daysPostpartum < 0) daysPostpartum = 0;

            string phase = "Giai đoạn hồi phục cấp tính (Tuần 1)";
            string initialRec = "Hãy ưu tiên nghỉ ngơi hoàn toàn tại giường, bổ sung đủ nước và tránh mang vác vật nặng.";

            if (daysPostpartum > 7 && daysPostpartum <= 42)
            {
                phase = "Giai đoạn phục hồi vết thương (Tuần 2 - 6)";
                initialRec = deliveryType.Equals("cesarean", StringComparison.OrdinalIgnoreCase)
                    ? "Vết mổ của bạn đang lành. Tập đi bộ nhẹ nhàng và tránh các động tác căng cơ bụng."
                    : "Tập Kegel nhẹ nhàng để hồi phục cơ sàn chậu và tăng tuần hoàn máu vùng chậu.";
            }
            else if (daysPostpartum > 42)
            {
                phase = "Giai đoạn ổn định lâu dài (Sau 6 tuần)";
                initialRec = "Bạn đã có thể bắt đầu tập luyện cường độ vừa phải. Hãy duy trì thói quen ăn uống cân bằng dinh dưỡng.";
            }

            var result = new
            {
                DaysPostpartum = daysPostpartum,
                RecoveryPhase = phase,
                InitialRecommendations = initialRec
            };

            return ApiResponse<object>.SuccessResult(result, "Thiết lập trạng thái sau sinh thành công.");
        }

        public async Task<ApiResponse<object>> SubmitEpdsAsync(string userId, int[] answers)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return ApiResponse<object>.FailureResult("Vui lòng tạo hồ sơ sức khỏe trước khi thực hiện khảo sát.");
            }

            try
            {
                var evaluation = _businessRuleEngine.EvaluateEpdsScore(answers);

                string aiMessage = evaluation.Recommendation;
                if (evaluation.IsUrgent)
                {
                    var profileDesc = $"Mom ID: {userId}, DeliveryDate: {profile.DeliveryDate:yyyy-MM-dd}";
                    aiMessage = await _geminiService.GenerateEpdsResponseAsync(evaluation.TotalScore, profileDesc);
                }

                var epds = new EpdsAssessment
                {
                    ProfileId = profile.Id,
                    Answers = answers,
                    TakenAt = DateTime.UtcNow,
                    AiAnalysis = $"Score: {evaluation.TotalScore}. Analysis: {aiMessage}"
                };

                await _epdsRepo.AddAsync(epds);
                await _epdsRepo.SaveChangesAsync();

                await _businessRuleEngine.EvaluateAsync(profile);

                var resources = new List<string>
                {
                    "Đường dây nóng hỗ trợ tâm lý sản phụ: 1900xxxx",
                    "Chương trình đồng hành sức khỏe tinh thần MomOi",
                    "Bác sĩ chuyên khoa tâm lý Bệnh viện Phụ sản"
                };

                var result = new
                {
                    Score = evaluation.TotalScore,
                    IsUrgent = evaluation.IsUrgent,
                    AiMessage = aiMessage,
                    Resources = resources
                };

                return ApiResponse<object>.SuccessResult(result, "Ghi nhận kết quả khảo sát EPDS thành công.");
            }
            catch (ArgumentException ex)
            {
                return ApiResponse<object>.FailureResult(ex.Message);
            }
        }

        public async Task<ApiResponse<VoiceJournalResult>> AnalyzeVoiceJournalAsync(string userId, string audioBase64, string mimeType)
        {
            var analysis = await _geminiService.AnalyzeVoiceJournalAsync(audioBase64, mimeType);
            return ApiResponse<VoiceJournalResult>.SuccessResult(analysis, "Phân tích nhật ký ghi âm thành công.");
        }

        public async Task<ApiResponse<object>> LogBreastfeedingAsync(string userId, string side, int durationMinutes, DateTime time)
        {
            var postpartumLog = new PostpartumLog
            {
                UserId = userId,
                DaysPostpartum = 10,
                RecordedAt = DateTime.UtcNow,
                Notes = $"Bú sữa bên: {side}, Thời lượng: {durationMinutes} phút, Lúc: {time:HH:mm}"
            };

            await _logRepo.AddAsync(postpartumLog);
            await _logRepo.SaveChangesAsync();

            var result = new
            {
                DailySummary = $"Hôm nay bé đã bú mẹ tổng cộng {durationMinutes} phút.",
                SupplyTrend = "Lượng sữa đang duy trì ở mức ổn định. Tiếp tục cho bé bú theo nhu cầu để duy trì nguồn sữa.",
                Tips = new[]
                {
                    "Massage bầu ngực nhẹ nhàng bằng khăn ấm trước khi cho bú.",
                    "Uống 1 cốc nước ấm trước và sau mỗi lần cho con bú.",
                    "Đảm bảo khớp ngậm của bé chính xác để không bị đau nứt cổ gà."
                }
            };

            return ApiResponse<object>.SuccessResult(result, "Lưu nhật ký cho con bú thành công.");
        }

        public async Task<ApiResponse<object>> GetRecoveryPlanAsync(string userId, int? day)
        {
            await Task.CompletedTask;
            int targetDay = day ?? 14;

            object exerciseSchedule;
            if (targetDay <= 7)
            {
                exerciseSchedule = new
                {
                    Week = 1,
                    ActiveExercise = "Tập bài tập sàn chậu nhẹ nhàng (Kegel nằm ngửa) và các động tác hít thở bằng bụng sâu.",
                    Allowed = new[] { "Kegel nhẹ", "Co duỗi chân" },
                    Locked = new[] { "Đi bộ đường dài", "Bài tập bụng sâu" }
                };
            }
            else if (targetDay <= 42)
            {
                exerciseSchedule = new
                {
                    Week = 2,
                    ActiveExercise = "Đi bộ nhẹ nhàng từ 15-20 phút mỗi ngày và tăng dần cường độ bài tập sàn chậu.",
                    Allowed = new[] { "Kegel nâng cao", "Đi bộ thong thả", "Tư thế mèo bò nhẹ" },
                    Locked = new[] { "Plank", "Crunch cơ bụng" }
                };
            }
            else
            {
                exerciseSchedule = new
                {
                    Week = 6,
                    ActiveExercise = "Bắt đầu tập cơ bụng cốt lõi (Core) nhẹ nhàng, đi bộ nhanh và tập các bài tập tay vai nhẹ.",
                    Allowed = new[] { "Plank ngắn", "Đi bộ nhanh", "Pilates hồi phục" },
                    Locked = new string[] { }
                };
            }

            var result = new
            {
                Day = targetDay,
                RecoveryPhase = targetDay <= 7 ? "Hồi phục ban đầu" : (targetDay <= 42 ? "Hồi phục trung gian" : "Ổn định lâu dài"),
                ExerciseSchedule = exerciseSchedule
            };

            return ApiResponse<object>.SuccessResult(result);
        }

        public async Task<ApiResponse<object>> GetLatestEpdsAsync(string userId)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy hồ sơ sức khỏe.");
            }

            var assessments = await _epdsRepo.FindAsync(e => e.ProfileId == profile.Id);
            var latest = assessments.OrderByDescending(e => e.TakenAt).FirstOrDefault();

            if (latest == null)
            {
                return ApiResponse<object>.SuccessResult(new { score = 0, isUrgent = false, takenAt = (DateTime?)null }, "Chưa thực hiện khảo sát EPDS nào.");
            }

            return ApiResponse<object>.SuccessResult(new
            {
                score = latest.TotalScore,
                isUrgent = latest.IsUrgent,
                takenAt = latest.TakenAt,
                aiAnalysis = latest.AiAnalysis
            });
        }
    }
}
