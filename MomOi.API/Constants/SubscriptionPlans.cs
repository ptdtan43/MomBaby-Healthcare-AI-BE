using MomOi.API.Models.Identity;
using System;
using System.Collections.Generic;

namespace MomOi.API.Constants
{
    public record SubscriptionPlan(
        string Code,
        SubscriptionTier Tier,
        int Months,
        decimal Price,
        string Name);

    public static class SubscriptionPlans
    {
        private static readonly Dictionary<string, SubscriptionPlan> Plans =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["HD_1M"] = new("HD_1M", SubscriptionTier.MomHienDai, 1, 99_000m, "Mom Hien Dai - 1 thang"),
                ["HD_6M"] = new("HD_6M", SubscriptionTier.MomHienDai, 6, 499_000m, "Mom Hien Dai - 6 thang"),
                ["VIP_1M"] = new("VIP_1M", SubscriptionTier.SuperMomVip, 1, 199_000m, "SuperMom VIP - 1 thang"),
                ["VIP_6M"] = new("VIP_6M", SubscriptionTier.SuperMomVip, 6, 999_000m, "SuperMom VIP - 6 thang"),
            };

        public static SubscriptionPlan? Get(string? code) =>
            string.IsNullOrWhiteSpace(code) ? null : Plans.GetValueOrDefault(code);

        public static IReadOnlyCollection<SubscriptionPlan> List() => Plans.Values;
    }
}
