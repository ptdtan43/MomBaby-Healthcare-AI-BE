using MomOi.API.DTOs;
using MomOi.API.DTOs.Mom;
using System.Threading.Tasks;

namespace MomOi.API.Services.UserProfile
{
    public interface IUserProfileService
    {
        Task<ApiResponse<object>> GetProfileAsync(string userId);
        Task<ApiResponse<object>> UpdateProfileAsync(string userId, UpdateMomHealthProfileDto dto);
    }
}
