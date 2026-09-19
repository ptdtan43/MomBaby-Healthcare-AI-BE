using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/wellness")]
    public class WellnessController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WellnessController(AppDbContext context)
        {
            _context = context;
        }

        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet("care-calendar")]
        public async Task<IActionResult> GetCareCalendar()
        {
            var profile = await _context.MomHealthProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
            var stage = profile?.Stage ?? JourneyStage.PrePregnancy;

            var items = stage switch
            {
                JourneyStage.Pregnant => new[]
                {
                    new { time = "Hôm nay", title = "Theo dõi chỉ số mẹ", description = "Uống đủ nước, ghi cân nặng và theo dõi dấu hiệu bất thường.", priority = 1 },
                    new { time = "Tuần này", title = "Mốc phát triển thai kỳ", description = "Xem mốc phát triển của bé và kiểm tra thực đơn dinh dưỡng.", priority = 2 },
                    new { time = "Lịch gần nhất", title = "Chuẩn bị lần khám", description = "Ghi lại câu hỏi cần trao đổi với bác sĩ ở lần khám thai tiếp theo.", priority = 3 }
                },
                JourneyStage.Postpartum => new[]
                {
                    new { time = "Hôm nay", title = "Theo dõi hồi phục", description = "Theo dõi sản dịch, nhiệt độ cơ thể và mức đau sau sinh.", priority = 1 },
                    new { time = "2 ngày tới", title = "Sức khỏe tinh thần", description = "Đánh giá EPDS hoặc ghi cảm xúc nếu thấy quá tải.", priority = 2 },
                    new { time = "Tuần này", title = "Chăm bé", description = "Ghi nhận chỉ số bé, cữ bú/tã và lịch tái khám mẹ bé.", priority = 3 }
                },
                _ => new[]
                {
                    new { time = "Hôm nay", title = "Ghi nhận chu kỳ", description = "Ghi nhận kỳ kinh, triệu chứng và chất nhầy cổ tử cung nếu có.", priority = 1 },
                    new { time = "2 ngày tới", title = "Cửa sổ thụ thai", description = "Theo dõi cửa sổ rụng trứng và nhắc uống acid folic.", priority = 2 },
                    new { time = "Tuần này", title = "Sẵn sàng trước thai kỳ", description = "Kiểm tra giấc ngủ, stress và lịch khám tiền thai nếu đang chuẩn bị.", priority = 3 }
                }
            };

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                stage = stage.ToString(),
                pregnancyWeek = profile?.PregnancyWeek,
                generatedAt = DateTime.UtcNow,
                items
            }));
        }

        [HttpGet("emergency-guide")]
        public IActionResult GetEmergencyGuide()
        {
            var redFlags = new[]
            {
                "Ra máu nhiều, đau bụng dữ dội hoặc co giật.",
                "Khó thở, đau ngực, ngất xỉu hoặc sốt cao liên tục.",
                "Sau sinh có ý nghĩ làm hại bản thân hoặc em bé.",
                "Bé bỏ bú, tím tái, sốt cao, li bì hoặc vàng da tăng nhanh."
            };

            var actions = new[]
            {
                "Gọi người thân ở gần và không ở một mình nếu triệu chứng đang nặng lên.",
                "Chuẩn bị giấy tờ khám, sổ thai/sổ bé, thuốc đang dùng và thời điểm triệu chứng bắt đầu.",
                "Liên hệ bác sĩ hoặc cơ sở y tế gần nhất để được hướng dẫn trực tiếp."
            };

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                headline = "Trợ giúp khẩn cấp",
                notice = "Khi có nguy hiểm tức thời, hãy gọi cấp cứu hoặc đến cơ sở y tế gần nhất.",
                redFlags,
                actions,
                disclaimer = "Mom Ơi chỉ hỗ trợ theo dõi và gợi ý chăm sóc. Với triệu chứng cấp cứu, quyết định y tế cần đến từ bác sĩ hoặc nhân viên y tế."
            }));
        }

        [HttpGet("relax-tracks")]
        public IActionResult GetRelaxTracks()
        {
            var tracks = new List<object>
            {
                new
                {
                    id = "piano-lullaby",
                    title = "Piano nhẹ cho mẹ nghỉ",
                    description = "Giai điệu piano chậm, ấm và ít nốt để thư giãn trước khi ngủ.",
                    durationMinutes = 15,
                    mode = "piano",
                    modeLabel = "Piano dịu",
                    tone = 261.63,
                    bpm = 52,
                    chords = new[] { 0, 7, 9, 5 },
                    noise = 0.03,
                    color = "pink",
                    audioUrl = (string?)null
                },
                new
                {
                    id = "lofi-mom-care",
                    title = "Beat nhẹ chăm sóc mẹ",
                    description = "Beat lofi rất mềm, bass thấp và piano rải hợp âm nhẹ.",
                    durationMinutes = 12,
                    mode = "lofi",
                    modeLabel = "Lofi beat",
                    tone = 220.0,
                    bpm = 64,
                    chords = new[] { 0, 3, 7, 5 },
                    noise = 0.05,
                    color = "violet",
                    audioUrl = (string?)null
                },
                new
                {
                    id = "rain-piano",
                    title = "Mưa nhỏ & piano xa",
                    description = "Nền mưa mỏng kết hợp tiếng piano thưa để giảm căng thẳng.",
                    durationMinutes = 10,
                    mode = "nature-piano",
                    modeLabel = "Mưa + piano",
                    tone = 196.0,
                    bpm = 48,
                    chords = new[] { 0, 5, 9, 7 },
                    noise = 0.16,
                    color = "emerald",
                    audioUrl = (string?)null
                }
            };

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                title = "Âm thanh phục hồi thư giãn",
                description = "Thư viện piano, beat nhẹ và âm thanh thiên nhiên hỗ trợ thư giãn, không thay thế trị liệu y khoa.",
                tracks
            }));
        }
    }
}
