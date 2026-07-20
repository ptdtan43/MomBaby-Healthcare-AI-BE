using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Admin;
using MomOi.API.DTOs.Auth;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Repositories;
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
            // Read active logs from CriticalAlertLogs
            var criticalLogs = await _context.CriticalAlertLogs
                .Where(c => !c.IsResolved)
                .ToListAsync();

            // Read pending alerts from LifestyleAlerts
            var lifestyleAlerts = await _context.LifestyleAlerts
                .Where(a => a.Status == AlertStatus.Pending && (a.Severity == AlertSeverity.High || a.Severity == AlertSeverity.Critical))
                .ToListAsync();

            // Combine all alert items into a standardized internal representation
            var combinedAlerts = criticalLogs.Select(c => new
            {
                c.UserId,
                Title = c.TitleVi,
                Message = c.MessageVi,
                c.Severity,
                c.TriggeredAt
            }).Concat(lifestyleAlerts.Select(l => new
            {
                l.UserId,
                l.Title,
                l.Message,
                l.Severity,
                l.TriggeredAt
            })).ToList();

            var userGroups = combinedAlerts.GroupBy(a => a.UserId).ToList();
            var userIds = userGroups.Select(g => g.Key).ToList();

            var usersMap = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.Email });

            var resultList = new System.Collections.Generic.List<object>();

            foreach (var group in userGroups)
            {
                var userId = group.Key;
                usersMap.TryGetValue(userId, out var userInfo);

                var latestAlert = group.OrderByDescending(a => a.TriggeredAt).First();

                // Aggregate distinct alert titles/reasons for this user
                var distinctReasons = group
                    .Select(a => string.IsNullOrEmpty(a.Title) ? a.Message : a.Title)
                    .Distinct()
                    .ToList();

                string consolidatedReason = string.Join(" • ", distinctReasons);

                // Determine highest severity
                var highestSeverity = group.Any(g => g.Severity == AlertSeverity.Critical) 
                    ? AlertSeverity.Critical 
                    : (group.Any(g => g.Severity == AlertSeverity.High) ? AlertSeverity.High : AlertSeverity.Warning);

                resultList.Add(new
                {
                    id = userId,
                    userId = userId,
                    fullName = userInfo?.FullName ?? userInfo?.Email ?? "Mẹ bầu",
                    email = userInfo?.Email ?? "N/A",
                    alertReason = consolidatedReason,
                    severity = highestSeverity.ToString().ToUpper(),
                    updatedAt = latestAlert.TriggeredAt.ToString("yyyy-MM-dd HH:mm")
                });
            }

            return ApiResponse<object>.SuccessResult(
                resultList.OrderByDescending(x => ((dynamic)x).updatedAt), 
                "Lấy danh sách người dùng có nguy cơ cao thành công."
            );
        }

        public async Task<ApiResponse<object>> GetReportsSummaryAsync()
        {
            var lifestyle = await _context.LifestyleEntries.ToListAsync();

            var stressDist = new
            {
                StressLow = lifestyle.Count(l => l.StressLevel == StressLevel.Low),
                StressModerate = lifestyle.Count(l => l.StressLevel == StressLevel.Moderate),
                StressHigh = lifestyle.Count(l => l.StressLevel == StressLevel.High)
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
