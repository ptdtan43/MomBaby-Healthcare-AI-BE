using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<ApiResponse<object>> GetUserDashboardAsync(string userId);
    }
}
