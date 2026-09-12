using MomOi.API.Models.Identity;
using System;

namespace MomOi.API.DTOs.Payment
{
    public class PaymentStatusDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? PaidAt { get; set; }

        /// <summary>Tier và hạn dùng của tài khoản sau khi giao dịch được xử lý.</summary>
        public SubscriptionTier CurrentTier { get; set; }
        public DateTime? TierExpiresAt { get; set; }
    }
}
