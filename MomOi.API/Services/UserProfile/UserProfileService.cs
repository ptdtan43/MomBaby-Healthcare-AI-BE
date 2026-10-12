using MomOi.API.DTOs;
using MomOi.API.DTOs.Mom;
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

        public async Task<ApiResponse<object>> UpdateProfileAsync(string userId, UpdateMomHealthProfileDto dto)
        {
            var profile = await _unitOfWork.Repository<MomHealthProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy hồ sơ sức khỏe.");
            }

            // Non-nullable columns: only an explicit non-null value changes them.
            if (dto.Stage.HasValue) profile.Stage = dto.Stage.Value;
            if (dto.HasGestDiabetes.HasValue) profile.HasGestDiabetes = dto.HasGestDiabetes.Value;
            if (dto.IsBreastfeeding.HasValue) profile.IsBreastfeeding = dto.IsBreastfeeding.Value;

            // Nullable columns: present-with-null clears, absent keeps.
            if (dto.Has(nameof(dto.PregnancyWeek))) profile.PregnancyWeek = dto.PregnancyWeek;
            if (dto.Has(nameof(dto.Bmi))) profile.Bmi = dto.Bmi;
            if (dto.Has(nameof(dto.Height))) profile.Height = dto.Height;
            if (dto.Has(nameof(dto.Weight))) profile.Weight = dto.Weight;
            if (dto.Has(nameof(dto.BloodSugarLevel))) profile.BloodSugarLevel = dto.BloodSugarLevel;
            if (dto.Has(nameof(dto.MedicalConditions))) profile.MedicalConditions = dto.MedicalConditions;
            if (dto.Has(nameof(dto.AvgCycleLength))) profile.AvgCycleLength = dto.AvgCycleLength;
            if (dto.Has(nameof(dto.LastPeriodDate)))
                profile.LastPeriodDate = dto.LastPeriodDate.HasValue
                    ? DateTime.SpecifyKind(dto.LastPeriodDate.Value, DateTimeKind.Utc)
                    : null;
            if (dto.Has(nameof(dto.DeliveryDate)))
                profile.DeliveryDate = dto.DeliveryDate.HasValue
                    ? DateTime.SpecifyKind(dto.DeliveryDate.Value, DateTimeKind.Utc)
                    : null;

            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(profile, "Cập nhật hồ sơ sức khỏe thành công.");
        }
    }
}
