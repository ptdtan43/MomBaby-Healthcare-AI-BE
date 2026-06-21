using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Admin
{
    public interface IAdminService
    {
        Task<ApiResponse<object>> GetUsersAtRiskAsync();
        Task<ApiResponse<object>> GetReportsSummaryAsync();
    }
}
