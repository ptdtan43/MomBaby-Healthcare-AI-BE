using Microsoft.AspNetCore.Http;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MomOi.API.Services.Payment
{
    /// <summary>Kết quả trả về cho VNPay trên kênh IPN.</summary>
    public record IpnResult(string RspCode, string Message);

    public interface IPaymentService
    {
        IReadOnlyCollection<PlanDto> GetPlans();

        Task<ApiResponse<CreatePaymentResponseDto>> CreatePaymentAsync(
            string userId, CreatePaymentRequestDto dto, string clientIp);

        /// <summary>
        /// Kênh duy nhất được phép đổi trạng thái giao dịch và nâng tier.
        /// </summary>
        Task<IpnResult> HandleVnPayIpnAsync(IQueryCollection query);

        Task<IpnResult> HandleMoMoIpnAsync(MoMoIpnDto dto);

        Task<ApiResponse<PaymentStatusDto>> GetStatusAsync(string userId, string orderCode);

        Task<ApiResponse<object>> GetHistoryAsync(string userId);
    }
}
