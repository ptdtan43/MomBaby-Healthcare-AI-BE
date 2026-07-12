using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MomOi.API.Services.Medication
{
    public class MedicationService : IMedicationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MedicationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<object>> AddMedicationScheduleAsync(string userId, MedicationScheduleRequestDto request)
        {
            var timeRegex = new Regex(@"^\d{2}:\d{2}$");
            if (!request.Times.Any() || request.Times.Any(t => !timeRegex.IsMatch(t)))
            {
                return ApiResponse<object>.FailureResult("Thời gian nhắc nhở phải có định dạng 'HH:mm' (ví dụ: '07:00').");
            }

            if (request.EndDate <= request.StartDate)
            {
                return ApiResponse<object>.FailureResult("Ngày kết thúc phải sau ngày bắt đầu.");
            }

            var schedule = new MedicationSchedule
            {
                UserId = userId,
                MedName = request.MedName,
                Dosage = request.Dosage,
                Times = request.Times,
                StartDate = request.StartDate.ToUniversalTime(),
                EndDate = request.EndDate.ToUniversalTime(),
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<MedicationSchedule>().AddAsync(schedule);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(schedule, "Tạo lịch uống thuốc thành công.");
        }

        public async Task<ApiResponse<object>> GetUserMedicationsAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var meds = await _unitOfWork.Repository<MedicationSchedule>()
                .FindAsync(m => m.UserId == userId && m.EndDate >= now);

            // Load adherence logs for all meds
            var scheduleIds = meds.Select(m => m.Id).ToList();
            var adherenceLogs = await _unitOfWork.Repository<MedicationAdherenceLog>()
                .FindAsync(l => scheduleIds.Contains(l.MedicationScheduleId));

            // Attach adherence logs to each schedule
            foreach (var med in meds)
            {
                med.AdherenceLogs = adherenceLogs
                    .Where(l => l.MedicationScheduleId == med.Id)
                    .ToList();
            }

            var result = meds.OrderBy(m => m.StartDate).ToList();
            return ApiResponse<object>.SuccessResult(result);
        }

        public async Task<ApiResponse<object>> UpdateAdherenceAsync(string userId, int id, AdherenceRequestDto request)
        {
            if (request.Status != "taken" && request.Status != "skipped")
            {
                return ApiResponse<object>.FailureResult("Trạng thái phải là 'taken' hoặc 'skipped'.");
            }

            var schedule = await _unitOfWork.Repository<MedicationSchedule>()
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (schedule == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy lịch uống thuốc.");
            }

            var targetDate = request.Date.Date;
            if (targetDate < schedule.StartDate.Date || targetDate > schedule.EndDate.Date)
            {
                return ApiResponse<object>.FailureResult("Ngày nằm ngoài khoảng thời gian của lịch uống thuốc.");
            }

            var existing = await _unitOfWork.Repository<MedicationAdherenceLog>()
                .FirstOrDefaultAsync(l => l.MedicationScheduleId == schedule.Id && l.Date.Date == targetDate);

            if (existing != null)
            {
                existing.Status = Enum.TryParse<AdherenceStatus>(request.Status, true, out var s2) ? s2 : AdherenceStatus.Taken;
                _unitOfWork.Repository<MedicationAdherenceLog>().Update(existing);
            }
            else
            {
                await _unitOfWork.Repository<MedicationAdherenceLog>().AddAsync(new MedicationAdherenceLog
                {
                    MedicationScheduleId = schedule.Id,
                    Date = targetDate,
                    Status = Enum.TryParse<AdherenceStatus>(request.Status, true, out var s1) ? s1 : AdherenceStatus.Taken
                });
            }

            schedule.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(null!, "Cập nhật trạng thái uống thuốc thành công.");
        }

        public async Task<ApiResponse<object>> DeleteMedicationScheduleAsync(string userId, int id)
        {
            var schedule = await _unitOfWork.Repository<MedicationSchedule>()
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (schedule == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy lịch uống thuốc.");
            }

            _unitOfWork.Repository<MedicationSchedule>().Remove(schedule);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(null!, "Xoá lịch uống thuốc thành công.");
        }
    }
}
