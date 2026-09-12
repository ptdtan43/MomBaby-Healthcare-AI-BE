using MomOi.API.Models.Identity;
using System;
using System.Collections.Generic;

namespace MomOi.API.Constants
{
    /// <summary>
    /// Một gói thuê bao bán ra: mã gói, tier được cấp, thời hạn và giá.
    /// </summary>
    public record SubscriptionPlan(
        string Code,
        SubscriptionTier Tier,
        int Months,
        decimal Price,
        string Name);

    /// <summary>
    /// Bảng giá nằm ở phía server. Client chỉ gửi lên mã gói, mọi thông tin còn lại
    /// tra ở đây — nếu để client gửi số tiền thì khách sửa được giá trước khi thanh toán.
    /// </summary>
    public static class SubscriptionPlans
    {
        private static readonly Dictionary<string, SubscriptionPlan> Plans =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["HD_1M"] = new("HD_1M", SubscriptionTier.MomHienDai, 1, 49_000m, "Mom Hiện Đại · 1 tháng"),
                ["HD_6M"] = new("HD_6M", SubscriptionTier.MomHienDai, 6, 249_000m, "Mom Hiện Đại · 6 tháng"),
                ["VIP_1M"] = new("VIP_1M", SubscriptionTier.SuperMomVip, 1, 99_000m, "SuperMom VIP · 1 tháng"),
                ["VIP_6M"] = new("VIP_6M", SubscriptionTier.SuperMomVip, 6, 499_000m, "SuperMom VIP · 6 tháng"),
            };

        public static SubscriptionPlan? Get(string? code) =>
            string.IsNullOrWhiteSpace(code) ? null : Plans.GetValueOrDefault(code);

        public static IReadOnlyCollection<SubscriptionPlan> List() => Plans.Values;
    }
}
