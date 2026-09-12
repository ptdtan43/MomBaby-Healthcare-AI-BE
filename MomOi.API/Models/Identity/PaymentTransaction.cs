using MomOi.API.Models.Identity;
using System;

namespace MomOi.API.Models.Identity
{
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class PaymentTransaction : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        public SubscriptionTier TargetTier { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentMethod { get; set; } = string.Empty;  // "VNPay", "MoMo", "ZaloPay"
        public string TransactionId { get; set; } = string.Empty;   // External payment ID
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? FailureReason { get; set; }
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// Mã đơn do MomOi sinh ra, gửi sang cổng làm vnp_TxnRef / orderId.
        /// Phải là duy nhất; cổng thanh toán từ chối mã trùng.
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Mã gói trong <see cref="Constants.SubscriptionPlans"/> tại thời điểm mua.
        /// </summary>
        public string PlanCode { get; set; } = string.Empty;

        /// <summary>
        /// Số tháng được cộng thêm khi thanh toán thành công. Lấy từ bảng giá, không từ client.
        /// </summary>
        public int DurationMonths { get; set; }

        /// <summary>
        /// Mã giao dịch phía cổng (vnp_TransactionNo / transId), dùng khi đối soát.
        /// </summary>
        public string? ProviderTxnNo { get; set; }

        /// <summary>
        /// Toàn bộ payload callback dạng JSON, giữ lại để tra cứu và làm bằng chứng khi khiếu nại.
        /// </summary>
        public string? RawCallback { get; set; }
    }
}
