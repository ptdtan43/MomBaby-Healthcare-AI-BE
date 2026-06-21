using MomOi.API.DTOs;
using MomOi.API.DTOs.Auth;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using System.Threading.Tasks;

namespace MomOi.API.Services.UserProfile
{
    public interface IUserProfileService
    {
        Task<ApiResponse<object>> GetProfileAsync(string userId);
        Task<ApiResponse<object>> UpdateProfileAsync(string userId, MomHealthProfile updateDto);
        Task<ApiResponse<object>> UpgradeSubscriptionAsync(string userId, SubscriptionTier tier);
    }
}
