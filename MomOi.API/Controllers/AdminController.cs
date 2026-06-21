using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Admin;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for admin dashboard and reporting.
    /// Migrated from Node.js adminController.js
    /// </summary>
    [Authorize] // In a real app, this should require an Admin role: [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// Retrieves users triggering HIGH-severity alerts.
        /// </summary>
        [HttpGet("users/risk")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsersAtRisk()
        {
            var response = await _adminService.GetUsersAtRiskAsync();
            return Ok(response);
        }

        /// <summary>
        /// Retrieves aggregated reporting stats (Stress distribution, Score trends, Top Rules).
        /// </summary>
        [HttpGet("reports/summary")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportsSummary()
        {
            var response = await _adminService.GetReportsSummaryAsync();
            return Ok(response);
        }
    }
}
