using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Payment;
using MomOi.API.Services.Payment;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        private string GetClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        /// <summary>Bảng giá để frontend vẽ màn hình chọn gói.</summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GetPlans() =>
            Ok(ApiResponse<object>.SuccessResult((object)_paymentService.GetPlans()));

        /// <summary>Tạo giao dịch và trả về liên kết thanh toán của cổng.</summary>
        [HttpPost("create")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<CreatePaymentResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreatePaymentRequestDto dto)
        {
            var response = await _paymentService.CreatePaymentAsync(GetUserId(), dto, GetClientIp());
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Nơi VNPay chuyển hướng trình duyệt khách về. Đường này đi qua máy khách nên
        /// giả mạo được — chỉ hiển thị kết quả, tuyệt đối không đổi dữ liệu ở đây.
        /// </summary>
        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult VnPayReturn([FromServices] VnPayGateway gateway)
        {
            if (!gateway.VerifyCallback(Request.Query))
                return BadRequest(ApiResponse<object>.FailureResult(
                    "Chữ ký không hợp lệ.", errorCode: "INVALID_SIGNATURE"));

            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var orderCode = Request.Query["vnp_TxnRef"].ToString();
            var succeeded = responseCode == "00";

            return Ok(ApiResponse<object>.SuccessResult((object)new
            {
                orderCode,
                responseCode,
                succeeded,
                note = "Trạng thái chính thức lấy qua GET /api/payment/status/{orderCode}."
            }, succeeded ? "Thanh toán thành công." : "Thanh toán không thành công."));
        }

        /// <summary>
        /// Kênh server-to-server của VNPay. Khách không can thiệp được, nên đây là nơi
        /// duy nhất ghi nhận thanh toán và nâng gói.
        /// </summary>
        [HttpGet("vnpay-ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            var result = await _paymentService.HandleVnPayIpnAsync(Request.Query);
            return Ok(new { RspCode = result.RspCode, Message = result.Message });
        }

        /// <summary>Nơi MoMo chuyển hướng khách về. Chỉ hiển thị, không đổi dữ liệu.</summary>
        [HttpGet("momo-return")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult MoMoReturn()
        {
            var resultCode = Request.Query["resultCode"].ToString();
            var succeeded = resultCode == "0";

            return Ok(ApiResponse<object>.SuccessResult((object)new
            {
                orderCode = Request.Query["orderId"].ToString(),
                resultCode,
                succeeded,
                note = "Trạng thái chính thức lấy qua GET /api/payment/status/{orderCode}."
            }, succeeded ? "Thanh toán thành công." : "Thanh toán không thành công."));
        }

        /// <summary>
        /// Kênh server-to-server của MoMo. Khác VNPay ở chỗ MoMo gửi POST JSON và
        /// chờ phản hồi 204 No Content chứ không đọc nội dung trả về.
        /// </summary>
        [HttpPost("momo-ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> MoMoIpn([FromBody] MoMoIpnDto dto)
        {
            await _paymentService.HandleMoMoIpnAsync(dto);
            return NoContent();
        }

        /// <summary>Trạng thái thật của giao dịch, đọc từ database.</summary>
        [HttpGet("status/{orderCode}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PaymentStatusDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatus(string orderCode)
        {
            var response = await _paymentService.GetStatusAsync(GetUserId(), orderCode);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}
