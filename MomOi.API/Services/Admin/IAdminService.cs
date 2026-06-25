using MomOi.API.DTOs;
using MomOi.API.DTOs.Auth;
using MomOi.API.DTOs.Admin;
using System.Threading.Tasks;

namespace MomOi.API.Services.Admin
{
    public interface IAdminService
    {
        Task<ApiResponse<object>> GetUsersAtRiskAsync();
        Task<ApiResponse<object>> GetReportsSummaryAsync();

        // User Management
        Task<ApiResponse<object>> GetAllUsersAsync();
        Task<ApiResponse<object>> CreateStaffOrExpertAsync(CreateStaffDto dto);
        Task<ApiResponse<object>> LockUserAsync(string userId);
        Task<ApiResponse<object>> UnlockUserAsync(string userId);

        // Sprint 3: Advanced Admin - Business Rules
        Task<ApiResponse<object>> GetBusinessRulesAsync();
        Task<ApiResponse<object>> CreateBusinessRuleAsync(BusinessRuleDto dto);
        Task<ApiResponse<object>> UpdateBusinessRuleAsync(int id, BusinessRuleDto dto);
        Task<ApiResponse<object>> DeleteBusinessRuleAsync(int id);

        // Sprint 3: USDA Sync
        Task<ApiResponse<object>> SyncUsdaDataAsync(UsdaSyncRequestDto dto);
    }
}
