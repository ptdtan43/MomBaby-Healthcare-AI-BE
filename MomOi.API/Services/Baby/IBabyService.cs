using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Services.BusinessRules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MomOi.API.Services.Baby
{
    public interface IBabyService
    {
        Task<ApiResponse<BabyProfile>> CreateBabyProfileAsync(string userId, BabyProfile profile);
        Task<ApiResponse<List<BabyProfile>>> GetBabyProfilesAsync(string userId);
        Task<ApiResponse<GrowthEvaluationResult>> LogGrowthAsync(string userId, int babyId, GrowthRecord record);
    }
}
