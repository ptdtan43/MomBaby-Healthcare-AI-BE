using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Report
{
    public interface IReportService
    {
        Task<ApiResponse<object>> GetUserReportDataAsync(string userId, int days = 30);
        ApiResponse<object> GenerateReportPDF();
    }
}
