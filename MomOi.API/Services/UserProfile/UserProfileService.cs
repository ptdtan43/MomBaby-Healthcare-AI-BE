using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Services.UserProfile
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserProfileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<object>> GetProfileAsync(string userId)
        {
            var profile = await _unitOfWork.Repository<MomHealthProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new MomHealthProfile
                {
                    UserId = userId,
                    Stage = JourneyStage.PrePregnancy,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<MomHealthProfile>().AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();
            }

            return ApiResponse<object>.SuccessResult(profile);
        }

        public async Task<ApiResponse<object>> UpdateProfileAsync(string userId, MomHealthProfile updateDto)
        {
            var profile = await _unitOfWork.Repository<MomHealthProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy hồ sơ sức khỏe.");
            }

            profile.Stage = updateDto.Stage;
            profile.PregnancyWeek = updateDto.PregnancyWeek;
            profile.Bmi = updateDto.Bmi;
            profile.Height = updateDto.Height;
            profile.Weight = updateDto.Weight;
            profile.BloodSugarLevel = updateDto.BloodSugarLevel;
            profile.HasGestDiabetes = updateDto.HasGestDiabetes;
            profile.MedicalConditions = updateDto.MedicalConditions;
            profile.AvgCycleLength = updateDto.AvgCycleLength;
            profile.LastPeriodDate = updateDto.LastPeriodDate.HasValue 
                ? DateTime.SpecifyKind(updateDto.LastPeriodDate.Value, DateTimeKind.Utc) 
                : null;
            profile.DeliveryDate = updateDto.DeliveryDate.HasValue 
                ? DateTime.SpecifyKind(updateDto.DeliveryDate.Value, DateTimeKind.Utc) 
                : null;
            profile.IsBreastfeeding = updateDto.IsBreastfeeding;
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(profile, "Cập nhật hồ sơ sức khỏe thành công.");
        }
    }
}
