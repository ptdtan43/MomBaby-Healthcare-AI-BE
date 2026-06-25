using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Admin;
using MomOi.API.DTOs.Auth;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Services.Integration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUsdaClientService _usdaClientService;

        public AdminService(
            AppDbContext context, 
            UserManager<AppUser> userManager,
            IUsdaClientService usdaClientService)
        {
            _context = context;
            _userManager = userManager;
            _usdaClientService = usdaClientService;
        }

        // ─── User Management ────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var result = new System.Collections.Generic.List<object>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.Tier,
                    u.CreatedAt,
                    IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                    Roles = roles
                });
            }
            return ApiResponse<object>.SuccessResult(result, "Lấy danh sách người dùng thành công.");
        }

        public async Task<ApiResponse<object>> CreateStaffOrExpertAsync(CreateStaffDto dto)
        {
            if (dto.Role != AppRoles.Staff && dto.Role != AppRoles.Expert)
                return ApiResponse<object>.FailureResult("Role không hợp lệ. Chỉ chấp nhận 'Staff' hoặc 'Expert'.");

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return ApiResponse<object>.FailureResult("Email này đã được sử dụng.");

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Tier = SubscriptionTier.SuperMomVip,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return ApiResponse<object>.FailureResult($"Tạo tài khoản thất bại: {errors}");
            }

            await _userManager.AddToRoleAsync(user, dto.Role);
            return ApiResponse<object>.SuccessResult(
                (object)new { user.Id, user.Email, user.FullName, Role = dto.Role },
                $"Tạo tài khoản {dto.Role} thành công.");
        }

        public async Task<ApiResponse<object>> LockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ApiResponse<object>.FailureResult("Không tìm thấy người dùng.");
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            return ApiResponse<object>.SuccessResult((object)"OK", "Khóa tài khoản thành công.");
        }

        public async Task<ApiResponse<object>> UnlockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ApiResponse<object>.FailureResult("Không tìm thấy người dùng.");
            await _userManager.SetLockoutEndDateAsync(user, null);
            return ApiResponse<object>.SuccessResult((object)"OK", "Mở khóa tài khoản thành công.");
        }

        // ─── Business Rules ──────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetBusinessRulesAsync()
        {
            var rules = await _context.BusinessRules.ToListAsync();
            return ApiResponse<object>.SuccessResult(rules, "Lấy danh sách Business Rules thành công.");
        }

        public async Task<ApiResponse<object>> CreateBusinessRuleAsync(BusinessRuleDto dto)
        {
            var rule = new BusinessRule
            {
                Code = dto.Code,
                Title = dto.Title,
                Description = dto.Description,
                TargetMetric = dto.TargetMetric,
                Operator = dto.Operator,
                ThresholdValue = dto.ThresholdValue,
                Severity = dto.Severity,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BusinessRules.Add(rule);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(rule, "Tạo Business Rule thành công.");
        }

        public async Task<ApiResponse<object>> UpdateBusinessRuleAsync(int id, BusinessRuleDto dto)
        {
            var rule = await _context.BusinessRules.FindAsync(id);
            if (rule == null) return ApiResponse<object>.FailureResult("Không tìm thấy Rule.");

            rule.Code = dto.Code;
            rule.Title = dto.Title;
            rule.Description = dto.Description;
            rule.TargetMetric = dto.TargetMetric;
            rule.Operator = dto.Operator;
            rule.ThresholdValue = dto.ThresholdValue;
            rule.Severity = dto.Severity;
            rule.IsActive = dto.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResponse<object>.SuccessResult(rule, "Cập nhật Business Rule thành công.");
        }

        public async Task<ApiResponse<object>> DeleteBusinessRuleAsync(int id)
        {
            var rule = await _context.BusinessRules.FindAsync(id);
            if (rule == null) return ApiResponse<object>.FailureResult("Không tìm thấy Rule.");

            _context.BusinessRules.Remove(rule);
            await _context.SaveChangesAsync();
            return ApiResponse<object>.SuccessResult((object)"OK", "Xóa Business Rule thành công.");
        }

        // ─── USDA Integration ────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> SyncUsdaDataAsync(UsdaSyncRequestDto dto)
        {
            return await _usdaClientService.SyncFoodsAsync(dto.Query, dto.MaxItems);
        }

        // ─── Dashboard & Reporting ───────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetUsersAtRiskAsync()
        {
            var highRiskAlerts = await _context.LifestyleAlerts
                .Where(a => a.Severity == AlertSeverity.High && a.Status == AlertStatus.Pending)
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TriggeredRules = g.ToList(),
                    LatestAlertDate = g.Max(a => a.TriggeredAt)
                })
                .ToListAsync();

            var userIds = highRiskAlerts.Select(a => a.UserId).ToList();
            var latestEntries = await _context.LifestyleEntries
                .Where(e => userIds.Contains(e.UserId))
                .GroupBy(e => e.UserId)
                .Select(g => g.OrderByDescending(e => e.Date).FirstOrDefault())
                .ToListAsync();

            var result = highRiskAlerts.Select(alert => new
            {
                userId = alert.UserId,
                triggeredRules = alert.TriggeredRules.Select(r => new { r.RuleId, r.Title }),
                healthScore = latestEntries.FirstOrDefault(e => e?.UserId == alert.UserId)?.HealthScore ?? 0,
                latestAlertDate = alert.LatestAlertDate
            }).OrderByDescending(r => r.latestAlertDate);

            return ApiResponse<object>.SuccessResult(result, "Lấy danh sách người dùng có nguy cơ cao thành công.");
        }

        public async Task<ApiResponse<object>> GetReportsSummaryAsync()
        {
            var stressGroups = await _context.LifestyleEntries
                .GroupBy(e => e.StressLevel)
                .Select(g => new { StressLevel = g.Key, Count = g.Count() })
                .ToListAsync();

            var stressDist = new
            {
                Low = stressGroups.FirstOrDefault(g => g.StressLevel == "Low")?.Count ?? 0,
                Moderate = stressGroups.FirstOrDefault(g => g.StressLevel == "Moderate")?.Count ?? 0,
                High = stressGroups.FirstOrDefault(g => g.StressLevel == "High")?.Count ?? 0
            };

            var scoreTrend = await _context.LifestyleEntries
                .GroupBy(e => e.Date.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    avgHealthScore = (int)Math.Round(g.Average(e => e.HealthScore))
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            var topRules = await _context.LifestyleAlerts
                .GroupBy(a => a.RuleId)
                .Select(g => new { ruleId = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            return ApiResponse<object>.SuccessResult(new
            {
                stressLevelDistribution = stressDist,
                healthScoreTrend = scoreTrend,
                topTriggeredRules = topRules
            }, "Thống kê báo cáo thành công.");
        }
    }
}
