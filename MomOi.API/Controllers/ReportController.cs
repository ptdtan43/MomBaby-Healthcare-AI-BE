using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Services.Report;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for generating health reports.
    /// Migrated from Node.js reportController.js
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/reports")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Retrieves aggregated health report data for the current user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserReportData([FromQuery] int days = 30)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _reportService.GetUserReportDataAsync(userId, days);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Placeholder for PDF generation endpoint.
        /// </summary>
        [HttpGet("pdf")]
        public IActionResult GenerateReportPDF()
        {
            var response = _reportService.GenerateReportPDF();
            return StatusCode(StatusCodes.Status501NotImplemented, response);
        }
    }
}
