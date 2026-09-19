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
                new { id = "rain-breath", title = "Mưa nhẹ & nhịp thở", durationMinutes = 10, tone = 196, modulation = 0.12, noise = 0.16, color = "pink" },
                new { id = "ocean-rest", title = "Sóng êm sau sinh", durationMinutes = 12, tone = 174, modulation = 0.08, noise = 0.22, color = "emerald" },
                new { id = "night-calm", title = "Ru ngủ dịu sâu", durationMinutes = 15, tone = 220, modulation = 0.05, noise = 0.1, color = "violet" }
            };

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                title = "Âm thanh phục hồi thư giãn",
                description = "Các track ambient được tạo trong trình duyệt để hỗ trợ thư giãn, không thay thế trị liệu y khoa.",
                tracks
            }));
        }
    }
}
