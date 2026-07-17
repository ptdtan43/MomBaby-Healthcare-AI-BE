using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.BusinessRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Baby
{
    public class BabyService : IBabyService
    {
        private readonly IGenericRepository<BabyProfile> _babyRepo;
        private readonly IGenericRepository<GrowthRecord> _growthRepo;
        private readonly IBusinessRuleEngine _businessRuleEngine;

        public BabyService(
            IGenericRepository<BabyProfile> babyRepo,
            IGenericRepository<GrowthRecord> growthRepo,
            IBusinessRuleEngine businessRuleEngine)
        {
            _babyRepo = babyRepo;
            _growthRepo = growthRepo;
            _businessRuleEngine = businessRuleEngine;
        }

        public async Task<ApiResponse<BabyProfile>> CreateBabyProfileAsync(string userId, BabyProfile profile)
        {
            profile.UserId = userId;
            await _babyRepo.AddAsync(profile);
            await _babyRepo.SaveChangesAsync();

            return ApiResponse<BabyProfile>.SuccessResult(profile, "Tạo hồ sơ cho bé thành công.");
        }

        public async Task<ApiResponse<List<BabyProfile>>> GetBabyProfilesAsync(string userId)
        {
            var profiles = (await _babyRepo.FindAsync(p => p.UserId == userId)).ToList();
            var profileIds = profiles.Select(p => p.Id).ToList();
            var growthRecords = (await _growthRepo.FindAsync(g => profileIds.Contains(g.BabyProfileId))).ToList();

            foreach (var profile in profiles)
            {
                profile.GrowthRecords = growthRecords.Where(g => g.BabyProfileId == profile.Id).ToList();
            }

            return ApiResponse<List<BabyProfile>>.SuccessResult(profiles);
        }

        public async Task<ApiResponse<GrowthEvaluationResult>> LogGrowthAsync(string userId, int babyId, GrowthRecord record)
        {
            var baby = await _babyRepo.FirstOrDefaultAsync(b => b.Id == babyId && b.UserId == userId);
            if (baby == null)
            {
                return ApiResponse<GrowthEvaluationResult>.FailureResult("Không tìm thấy hồ sơ của bé.");
            }

            record.BabyProfileId = baby.Id;
            record.BabyProfile = null!;
            record.RecordedAt = DateTime.UtcNow;

            await _growthRepo.AddAsync(record);

            baby.CurrentWeightKg = record.WeightKg;
            baby.CurrentHeightCm = record.HeightCm;

            _babyRepo.Update(baby);
            await _babyRepo.SaveChangesAsync();

            var evaluation = _businessRuleEngine.VerifyBabyGrowth(
                (int)((DateTime.UtcNow - baby.DateOfBirth).TotalDays / 30),
                baby.Gender.ToString(),
                baby.CurrentWeightKg ?? 0f,
                record.HeightCm
            );

            return ApiResponse<GrowthEvaluationResult>.SuccessResult(evaluation, "Ghi nhận chỉ số tăng trưởng và đánh giá thành công.");
        }

        public async Task<ApiResponse<BabyProfile>> UpdateBabyProfileAsync(string userId, int id, BabyProfile profile)
        {
            var existing = await _babyRepo.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (existing == null)
            {
                return ApiResponse<BabyProfile>.FailureResult("Không tìm thấy hồ sơ của bé.");
            }

            existing.BabyName = profile.BabyName;
            existing.DateOfBirth = profile.DateOfBirth;
            existing.Gender = profile.Gender;
            existing.CurrentWeightKg = profile.CurrentWeightKg;
            existing.CurrentHeightCm = profile.CurrentHeightCm;
            existing.Allergies = profile.Allergies;
            existing.FoodHistory = profile.FoodHistory;

            _babyRepo.Update(existing);
            await _babyRepo.SaveChangesAsync();

            return ApiResponse<BabyProfile>.SuccessResult(existing, "Cập nhật hồ sơ bé thành công.");
        }
    }
}
