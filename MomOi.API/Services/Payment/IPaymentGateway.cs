using MomOi.API.Models.Identity;
using System.Threading.Tasks;

namespace MomOi.API.Services.Payment
{
    /// <summary>
    /// Phần chung của mọi cổng thanh toán: đổi một giao dịch thành liên kết trả tiền.
    /// Khâu kiểm chữ ký nằm ở từng lớp cụ thể vì VNPay gửi callback bằng query string
    /// còn MoMo gửi bằng POST JSON — gộp vào đây chỉ làm giao diện rối thêm.
    /// </summary>
    public interface IPaymentGateway
    {
        string Provider { get; }

        Task<string> CreatePaymentUrlAsync(PaymentTransaction txn, string clientIp);
    }
}
