using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MomOi.API.Repositories;
using MomOi.API.DTOs.Auth;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Models.Nutrition;
using System;
using System.Collections.Generic;
using System.Linq;
using RecipeEntity = MomOi.API.Models.Health.Recipe;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MomOi.API.Services.Auth
{
    /// <summary>
    /// Handles authorization, user registration, JWT generation (RS256), and refresh token cycles.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email này đã được đăng ký sử dụng.");
            }

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                CreatedAt = DateTime.UtcNow,
                Tier = SubscriptionTier.Free
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", System.Linq.Enumerable.Select(result.Errors, e => e.Description));
                throw new InvalidOperationException($"Đăng ký thất bại: {errors}");
            }

            // Assign default Mom role
            await _userManager.AddToRoleAsync(user, AppRoles.Mom);

            // PII & Health separation: Initialize empty MomHealthProfile in health table
            var healthProfile = new MomHealthProfile
            {
                UserId = user.Id,
                Stage = JourneyStage.PrePregnancy,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<MomHealthProfile>().AddAsync(healthProfile);
            await _unitOfWork.SaveChangesAsync();

            var token = await GenerateJwtTokenAsync(user);
            var refreshToken = await IssueRefreshTokenAsync(user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    Tier = user.EffectiveTier,
                    TierExpiresAt = user.TierExpiresAt,
                    Roles = roles
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác.");
            }

            if (await _userManager.IsLockedOutAsync(user) || (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow))
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn đã bị khóa bởi Quản trị viên.");
            }

            var token = await GenerateJwtTokenAsync(user);
            var refreshToken = await IssueRefreshTokenAsync(user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    Tier = user.EffectiveTier,
                    TierExpiresAt = user.TierExpiresAt,
                    Roles = roles
                }
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
        {
            var tokens = _unitOfWork.Repository<UserRefreshToken>();
            var hash = HashToken(dto.RefreshToken);
            var stored = await tokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

            if (stored == null || stored.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh Token không hợp lệ hoặc đã hết hạn.");
            }

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Refresh Token không hợp lệ hoặc đã hết hạn.");
            }

            if (await _userManager.IsLockedOutAsync(user) || (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow))
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn đã bị khóa bởi Quản trị viên.");
            }

            // Rotation: the presented token is single-use; only this device's row is replaced.
            tokens.Remove(stored);

            var newToken = await GenerateJwtTokenAsync(user);
            var newRefreshToken = await IssueRefreshTokenAsync(user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            return new AuthResponseDto
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    Tier = user.EffectiveTier,
                    TierExpiresAt = user.TierExpiresAt,
                    Roles = roles
                }
            };
        }

        public async Task LogoutAsync(string userId, string? refreshToken)
        {
            var tokens = _unitOfWork.Repository<UserRefreshToken>();

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var hash = HashToken(refreshToken);
                var current = await tokens.FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == hash);
                if (current != null) tokens.Remove(current);
            }
            else
            {
                // Legacy clients send no token: revoke every device, as before.
                tokens.RemoveRange(await tokens.FindAsync(t => t.UserId == userId));
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> DeleteAccountAsync(string userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("Không tìm thấy tài khoản.");

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                return false;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await PurgeUserDataAsync(userId);

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", System.Linq.Enumerable.Select(result.Errors, e => e.Description));
                    throw new InvalidOperationException($"Xóa tài khoản thất bại: {errors}");
                }
            });

            return true;
        }

        /// <summary>
        /// Hard-deletes every row belonging to the user, children before parents.
        /// ExecuteDeleteAsync issues a SQL DELETE directly, so it bypasses the soft-delete
        /// conversion in SaveChanges; IgnoreQueryFilters makes it also reach rows that were
        /// already soft-deleted. The list is explicit on purpose: BabyProfile (and its
        /// children) have no FK to the user, so a database cascade alone would orphan them.
        /// When adding a table that stores user data, add it here.
        /// </summary>
        private async Task PurgeUserDataAsync(string userId)
        {
            IQueryable<T> Q<T>() where T : class => _unitOfWork.Repository<T>().Query().IgnoreQueryFilters();

            var babyIds = Q<BabyProfile>().Where(b => b.UserId == userId).Select(b => b.Id);
            await Q<GrowthRecord>().Where(g => babyIds.Contains(g.BabyProfileId)).ExecuteDeleteAsync();
            await Q<VaccinationRecord>().Where(v => babyIds.Contains(v.BabyProfileId)).ExecuteDeleteAsync();
            await Q<BabyFoodLog>().Where(f => f.UserId == userId || babyIds.Contains(f.BabyProfileId)).ExecuteDeleteAsync();
            await Q<BabyProfile>().Where(b => b.UserId == userId).ExecuteDeleteAsync();

            var profileIds = Q<MomHealthProfile>().Where(p => p.UserId == userId).Select(p => p.Id);
            await Q<CycleLog>().Where(c => profileIds.Contains(c.ProfileId)).ExecuteDeleteAsync();
            await Q<EpdsAssessment>().Where(e => profileIds.Contains(e.ProfileId)).ExecuteDeleteAsync();
            await Q<MomHealthProfile>().Where(p => p.UserId == userId).ExecuteDeleteAsync();

            await Q<MedicationAdherenceLog>().Where(l => l.Schedule.UserId == userId).ExecuteDeleteAsync();
            await Q<MedicationSchedule>().Where(m => m.UserId == userId).ExecuteDeleteAsync();

            await Q<ChatMessage>().Where(m => m.Session.UserId == userId).ExecuteDeleteAsync();
            await Q<ChatSession>().Where(s => s.UserId == userId).ExecuteDeleteAsync();

            await Q<SymptomLog>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<DailyMonitoringLog>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<LifestyleEntry>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<LifestyleAlert>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<CriticalAlertLog>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<NotificationAlert>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<PregnancyLog>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<PostpartumLog>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<ExerciseLog>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<MealLog>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<FoodAllergyRecord>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<DietPlan>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<RecipeEntity>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await Q<UserRefreshToken>().Where(x => x.UserId == userId).ExecuteDeleteAsync();

            // Kept but unlinked: other users' recipes this expert reviewed, and payments.
            await Q<RecipeEntity>().Where(r => r.ReviewedByExpertId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ReviewedByExpertId, (string?)null));
            await Q<PaymentTransaction>().Where(p => p.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UserId, (string?)null));
        }

        private async Task<string> IssueRefreshTokenAsync(string userId)
        {
            var tokens = _unitOfWork.Repository<UserRefreshToken>();
            var now = DateTime.UtcNow;

            tokens.RemoveRange(await tokens.FindAsync(t => t.UserId == userId && t.ExpiresAt <= now));

            var refreshToken = GenerateRefreshToken();
            await tokens.AddAsync(new UserRefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(refreshToken),
                ExpiresAt = now.AddDays(30)
            });
            await _unitOfWork.SaveChangesAsync();

            return refreshToken;
        }

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private async Task<string> GenerateJwtTokenAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("fullname", user.FullName),
                new Claim("tier", ((int)user.EffectiveTier).ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            var signingKey = RsaKeyHelper.GetSigningKey(_configuration);
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15), // 15-minute access token
                Issuer = _configuration["Jwt:Issuer"] ?? "MomOiAPI",
                Audience = _configuration["Jwt:Audience"] ?? "MomOiFrontend",
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
