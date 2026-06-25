using MomOi.API.DTOs;
using MomOi.API.DTOs.Mom;
using System.Threading.Tasks;

namespace MomOi.API.Services.Mom
{
    public interface IMomService
    {
        // ─── Allergies ──────────────────────────────────────────────────────────
        Task<ApiResponse<object>> GetAllergiesAsync(string userId);
        Task<ApiResponse<object>> AddAllergyAsync(string userId, CreateAllergyDto dto);
        Task<ApiResponse<object>> RemoveAllergyAsync(string userId, int allergyId);

        // ─── Diet Plans ─────────────────────────────────────────────────────────
        Task<ApiResponse<object>> GetDietPlansAsync(string userId);
        Task<ApiResponse<object>> CreateManualDietPlanAsync(string userId, CreateDietPlanDto dto);
        Task<ApiResponse<object>> GenerateAIDietPlanAsync(string userId, GenerateDietPlanDto dto);

        // ─── Premium Upgrade ────────────────────────────────────────────────────
        Task<ApiResponse<object>> UpgradeToPremiumAsync(string userId, UpgradePremiumDto dto);
    }
}
