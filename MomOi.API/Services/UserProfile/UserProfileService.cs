using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Auth;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Services.UserProfile
{
    public class UserProfileService : IUserProfileService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public UserProfileService(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApiResponse<object>> GetProfileAsync(string userId)
        {
            var profile = await _context.MomHealthProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new MomHealthProfile
                {
                    UserId = userId,
                    Stage = JourneyStage.PrePregnancy,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.MomHealthProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            return ApiResponse<object>.SuccessResult(profile);
        }

        public async Task<ApiResponse<object>> UpdateProfileAsync(string userId, MomHealthProfile updateDto)
        {
            var profile = await _context.MomHealthProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy hồ sơ sức khỏe.");
            }

            profile.Stage = updateDto.Stage;
            profile.PregnancyWeek = updateDto.PregnancyWeek;
            profile.Bmi = updateDto.Bmi;
            profile.BloodSugarLevel = updateDto.BloodSugarLevel;
            profile.HasGestDiabetes = updateDto.HasGestDiabetes;
            profile.MedicalConditions = updateDto.MedicalConditions;
            profile.AvgCycleLength = updateDto.AvgCycleLength;
            profile.LastPeriodDate = updateDto.LastPeriodDate;
            profile.DeliveryDate = updateDto.DeliveryDate;
            profile.IsBreastfeeding = updateDto.IsBreastfeeding;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(profile, "Cập nhật hồ sơ sức khỏe thành công.");
        }

        public async Task<ApiResponse<object>> UpgradeSubscriptionAsync(string userId, SubscriptionTier tier)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ApiResponse<object>.FailureResult("Người dùng không tồn tại.");

            user.Tier = tier;
            user.TierExpiresAt = DateTime.UtcNow.AddMonths(1); // 1 month subscription
            await _userManager.UpdateAsync(user);

            var response = new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email!,
                Tier = user.Tier
            };

            return ApiResponse<object>.SuccessResult(response, $"Nâng cấp thành công lên gói {tier}.");
        }
    }
}
