using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.DTOs.DoctorCallback;
using MomOi.API.DTOs.Payment;
using MomOi.API.Middleware;
using MomOi.API.Models.Identity;
using MomOi.API.Services.DoctorCallback;
using MomOi.API.Services.Payment;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// "Bác sĩ gọi lại" cho hội viên Super Mom VIP.
    /// Mẹ không bao giờ thấy số bác sĩ; phí 5.000đ chỉ tính khi bác sĩ xác nhận đã tư vấn xong.
    /// </summary>
    [ApiController]
    [Authorize]
    public class DoctorCallbackController : ControllerBase
    {
        private readonly IDoctorCallbackService _service;
        private readonly IPaymentService _paymentService;

        public DoctorCallbackController(IDoctorCallbackService service, IPaymentService paymentService)
        {
            _service = service;
            _paymentService = paymentService;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // ─── Phía mẹ ─────────────────────────────────────────────────────────────

        /// <summary>Gửi yêu cầu bác sĩ gọi lại. Chỉ Super Mom VIP.</summary>
        [HttpPost("api/doctor-callbacks")]
        [RequiresTier(SubscriptionTier.SuperMomVip)]
        [ProducesResponseType(typeof(ApiResponse<DoctorCallbackDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateDoctorCallbackDto dto)
        {
            var response = await _service.CreateAsync(GetUserId(), dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Danh sách yêu cầu của mẹ. Không gắn tier: gói hết hạn mẹ vẫn xem được
        /// lịch sử và thanh toán phí còn nợ.
        /// </summary>
        [HttpGet("api/doctor-callbacks/mine")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine() => Ok(await _service.GetMineAsync(GetUserId()));

        [HttpPost("api/doctor-callbacks/{id:int}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<DoctorCallbackDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Cancel(int id)
        {
            var response = await _service.CancelAsync(GetUserId(), id);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Tạo (hoặc lấy lại) mã VietQR 5.000đ cho cuộc tư vấn đã hoàn tất.
        /// Trạng thái thanh toán đọc qua GET /api/payment/status/{orderCode} như mua gói.
        /// </summary>
        [HttpPost("api/doctor-callbacks/{id:int}/pay")]
        [ProducesResponseType(typeof(ApiResponse<CreatePaymentResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Pay(int id)
        {
            var response = await _paymentService.CreateDoctorCallbackFeePaymentAsync(GetUserId(), id);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // ─── Phía bác sĩ / chuyên gia ────────────────────────────────────────────

        [HttpGet("api/expert/callbacks")]
        [Authorize(Roles = AppRoles.Expert + "," + AppRoles.Staff + "," + AppRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetQueue() => Ok(await _service.GetQueueAsync());

        /// <summary>Bác sĩ đã gọi và tư vấn xong: phát sinh phí 5.000đ cho mẹ.</summary>
        [HttpPost("api/expert/callbacks/{id:int}/complete")]
        [Authorize(Roles = AppRoles.Expert + "," + AppRoles.Staff + "," + AppRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<DoctorCallbackQueueItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Complete(int id, [FromBody] ResolveDoctorCallbackDto? dto)
        {
            var response = await _service.CompleteAsync(GetUserId(), id, dto?.Note);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>Gọi không được: đóng yêu cầu, không tính phí.</summary>
        [HttpPost("api/expert/callbacks/{id:int}/unreachable")]
        [Authorize(Roles = AppRoles.Expert + "," + AppRoles.Staff + "," + AppRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<DoctorCallbackQueueItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Unreachable(int id, [FromBody] ResolveDoctorCallbackDto? dto)
        {
            var response = await _service.MarkUnreachableAsync(GetUserId(), id, dto?.Note);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
