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
    }
}
