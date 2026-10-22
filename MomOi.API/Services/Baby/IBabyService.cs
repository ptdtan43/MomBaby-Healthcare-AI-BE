using MomOi.API.DTOs;
using MomOi.API.DTOs.Baby;
using MomOi.API.Models.Health;
using Microsoft.AspNetCore.Http;
using System.IO;
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

        /// <summary>
        /// Gets a personalized (allergen-free, WHO-compliant) daily or weekly menu for a baby
        /// by delegating to the Python nutrition recommendation engine.
        /// </summary>
        Task<ApiResponse<object>> GetBabyMenuAsync(string userId, int babyId, bool weekly, bool forceRefresh = false);
        Task<ApiResponse<BabyProfile>> UpdateBabyProfileAsync(string userId, int id, BabyProfile profile);
        Task<ApiResponse<object>> DeleteGrowthRecordAsync(string userId, int babyId, int recordId);
        Task<ApiResponse<BabyPhotoDto>> UploadBabyPhotoAsync(string userId, int babyId, IFormFile file);
        Task<BabyPhotoDownload> GetBabyPhotoAsync(string userId, int babyId);
        Task<ApiResponse<object>> DeleteBabyPhotoAsync(string userId, int babyId);
    }

    public sealed class BabyPhotoDownload
    {
        public ApiResponse<BabyPhotoDto> Response { get; init; } = null!;
        public Stream? Content { get; init; }
    }
}
