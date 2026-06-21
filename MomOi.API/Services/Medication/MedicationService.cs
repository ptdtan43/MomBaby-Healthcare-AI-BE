using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MomOi.API.Services.Medication
{
    public class MedicationService : IMedicationService
    {
        // For Medication, since it has complex includes (.Include(m => m.AdherenceLogs)),
        // it's easier to inject AppDbContext directly or use a specialized repository.
        // We will inject AppDbContext to maintain full EF Core power for related entities.
        private readonly AppDbContext _context;

        public MedicationService(AppDbContext context)
        {
            _context = context;
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

            _context.MedicationSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(schedule, "Tạo lịch uống thuốc thành công.");
        }

        public async Task<ApiResponse<object>> GetUserMedicationsAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var meds = await _context.MedicationSchedules
                .Where(m => m.UserId == userId && m.EndDate >= now)
                .Include(m => m.AdherenceLogs)
                .OrderBy(m => m.StartDate)
                .ToListAsync();

            return ApiResponse<object>.SuccessResult(meds);
        }

        public async Task<ApiResponse<object>> UpdateAdherenceAsync(string userId, int id, AdherenceRequestDto request)
        {
            if (request.Status != "taken" && request.Status != "skipped")
            {
                return ApiResponse<object>.FailureResult("Trạng thái phải là 'taken' hoặc 'skipped'.");
            }

            var schedule = await _context.MedicationSchedules
                .Include(m => m.AdherenceLogs)
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

            var existing = schedule.AdherenceLogs.FirstOrDefault(l => l.Date.Date == targetDate);
            if (existing != null)
            {
                existing.Status = request.Status;
                _context.MedicationAdherenceLogs.Update(existing);
            }
            else
            {
                _context.MedicationAdherenceLogs.Add(new MedicationAdherenceLog
                {
                    MedicationScheduleId = schedule.Id,
                    Date = targetDate,
                    Status = request.Status
                });
            }

            schedule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(null!, "Cập nhật trạng thái uống thuốc thành công.");
        }

        public async Task<ApiResponse<object>> DeleteMedicationScheduleAsync(string userId, int id)
        {
            var schedule = await _context.MedicationSchedules
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (schedule == null)
            {
                return ApiResponse<object>.FailureResult("Không tìm thấy lịch uống thuốc.");
            }

            _context.MedicationSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(null!, "Xoá lịch uống thuốc thành công.");
        }
    }
}
