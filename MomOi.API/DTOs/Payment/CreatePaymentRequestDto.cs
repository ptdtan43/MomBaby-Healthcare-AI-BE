using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.Payment
{
    public class CreatePaymentRequestDto
    {
        /// <summary>
        /// Mã gói trong bảng giá phía server, ví dụ "VIP_1M".
        /// Client không gửi số tiền — server tự tra giá để khách không sửa được.
        /// </summary>
        [Required(ErrorMessage = "Vui lòng chọn gói cần mua.")]
        public string PlanCode { get; set; } = string.Empty;

        /// <summary>Cổng thanh toán: "VNPay" hoặc "MoMo".</summary>
        public string Provider { get; set; } = "VNPay";
    }
}
