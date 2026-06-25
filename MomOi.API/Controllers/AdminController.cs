using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Auth;
using MomOi.API.Models.Identity;
using MomOi.API.Services.Admin;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for admin dashboard, reporting, and user management.
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ─── User Management ────────────────────────────────────────────────────

        /// <summary>Retrieves all users with their roles and lock status.</summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers()
        {
            var response = await _adminService.GetAllUsersAsync();
            return Ok(response);
        }

        /// <summary>Creates a new Staff or Expert account.</summary>
        [HttpPost("users/create")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateStaffOrExpert([FromBody] CreateStaffDto dto)
        {
            var response = await _adminService.CreateStaffOrExpertAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>Locks a user account indefinitely.</summary>
        [HttpPatch("users/{userId}/lock")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LockUser(string userId)
        {
            var response = await _adminService.LockUserAsync(userId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>Unlocks a previously locked user account.</summary>
        [HttpPatch("users/{userId}/unlock")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var response = await _adminService.UnlockUserAsync(userId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        // ─── Business Rules ──────────────────────────────────────────────────────

        /// <summary>Lấy danh sách toàn bộ Business Rules.</summary>
        [HttpGet("rules")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBusinessRules()
        {
            var response = await _adminService.GetBusinessRulesAsync();
            return Ok(response);
        }

        /// <summary>Tạo mới một Business Rule.</summary>
        [HttpPost("rules")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateBusinessRule([FromBody] MomOi.API.DTOs.Admin.BusinessRuleDto dto)
        {
            var response = await _adminService.CreateBusinessRuleAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>Cập nhật một Business Rule đã có.</summary>
        [HttpPut("rules/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateBusinessRule(int id, [FromBody] MomOi.API.DTOs.Admin.BusinessRuleDto dto)
        {
            var response = await _adminService.UpdateBusinessRuleAsync(id, dto);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>Xóa một Business Rule.</summary>
        [HttpDelete("rules/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteBusinessRule(int id)
        {
            var response = await _adminService.DeleteBusinessRuleAsync(id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        // ─── USDA Integration ────────────────────────────────────────────────────

        /// <summary>Kích hoạt đồng bộ dữ liệu dinh dưỡng từ USDA FoodData Central.</summary>
        [HttpPost("usda/sync")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SyncUsdaData([FromBody] MomOi.API.DTOs.Admin.UsdaSyncRequestDto dto)
        {
            var response = await _adminService.SyncUsdaDataAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // ─── Dashboard & Reporting ───────────────────────────────────────────────

        /// <summary>Retrieves users triggering HIGH-severity alerts.</summary>
        [HttpGet("users/risk")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsersAtRisk()
        {
            var response = await _adminService.GetUsersAtRiskAsync();
            return Ok(response);
        }

        /// <summary>Retrieves aggregated reporting stats (Stress distribution, Score trends, Top Rules).</summary>
        [HttpGet("reports/summary")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportsSummary()
        {
            var response = await _adminService.GetReportsSummaryAsync();
            return Ok(response);
        }
    }
}

