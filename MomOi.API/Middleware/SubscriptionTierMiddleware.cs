using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MomOi.API.DTOs;
using MomOi.API.Models.Identity;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Middleware
{
    /// <summary>
    /// Attribute used to gate controller actions based on required subscription tiers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresTierAttribute : Attribute
    {
        public SubscriptionTier MinimumTier { get; }

        public RequiresTierAttribute(SubscriptionTier minimumTier)
        {
            MinimumTier = minimumTier;
        }
    }

    /// <summary>
    /// Middleware to verify if the authenticated user has the necessary subscription tier to access the requested endpoint.
    /// </summary>
    public class SubscriptionTierMiddleware
    {
        private readonly RequestDelegate _next;

        public SubscriptionTierMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var requiresTierAttribute = endpoint?.Metadata.GetMetadata<RequiresTierAttribute>();

            if (requiresTierAttribute == null)
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated != true)
            {
                await DenyAsync(context, StatusCodes.Status401Unauthorized,
                    "Vui lòng đăng nhập để sử dụng tính năng này.");
                return;
            }

            // Tier đọc từ database chứ không từ claim "tier" trong JWT: token do lần đăng nhập
            // trước cấp không biết về giao dịch thanh toán vừa hoàn tất, lẫn về gói vừa hết hạn.
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userManager = context.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var user = string.IsNullOrEmpty(userId) ? null : await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                await DenyAsync(context, StatusCodes.Status401Unauthorized,
                    "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
                return;
            }

            if (user.EffectiveTier < requiresTierAttribute.MinimumTier)
            {
                await DenyAsync(context, StatusCodes.Status403Forbidden,
                    "Vui lòng nâng cấp gói để sử dụng tính năng này.");
                return;
            }

            await _next(context);
        }

        private static Task DenyAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var error = ApiResponse<object>.FailureResult(message);
            return context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
