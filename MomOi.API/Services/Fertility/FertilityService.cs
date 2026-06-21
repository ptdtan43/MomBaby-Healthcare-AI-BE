using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.BusinessRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Fertility
{
    public class FertilityService : IFertilityService
    {
        private readonly IGenericRepository<MomHealthProfile> _profileRepo;
        private readonly IGenericRepository<CycleLog> _cycleRepo;
        private readonly IBusinessRuleEngine _ruleEngine;

        public FertilityService(
            IGenericRepository<MomHealthProfile> profileRepo,
            IGenericRepository<CycleLog> cycleRepo,
            IBusinessRuleEngine ruleEngine)
        {
            _profileRepo = profileRepo;
            _cycleRepo = cycleRepo;
            _ruleEngine = ruleEngine;
        }

        public async Task<ApiResponse<object>> LogCycleAsync(string userId, DateTime periodStartDate, int cycleLength, string[] symptoms)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return ApiResponse<object>.FailureResult("Vui lòng tạo hồ sơ sức khỏe trước khi ghi chép chu kỳ.");
            }

            profile.LastPeriodDate = periodStartDate;
            profile.AvgCycleLength = cycleLength;
            profile.UpdatedAt = DateTime.UtcNow;

            var log = new CycleLog
            {
                ProfileId = profile.Id,
                StartDate = periodStartDate,
                EndDate = periodStartDate.AddDays(5),
                Symptoms = string.Join(",", symptoms)
            };

            await _cycleRepo.AddAsync(log);
            _profileRepo.Update(profile);
            await _profileRepo.SaveChangesAsync();

            var ovulationDay = periodStartDate.AddDays(cycleLength - 14);
            var result = new
            {
                OvulationDay = ovulationDay,
                FertileWindowStart = ovulationDay.AddDays(-5),
                FertileWindowEnd = ovulationDay.AddDays(1),
                PeriodPrediction = periodStartDate.AddDays(cycleLength)
            };

            return ApiResponse<object>.SuccessResult(result, "Ghi chép chu kỳ thành công và đã cập nhật dự báo thụ thai.");
        }

        public async Task<ApiResponse<object>> GetCalendarAsync(string userId, string month)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null || !profile.LastPeriodDate.HasValue)
            {
                return ApiResponse<object>.FailureResult("Vui lòng thiết lập ngày chu kỳ cuối cùng để xem lịch dự đoán.");
            }

            if (string.IsNullOrEmpty(month) || month.Length != 7)
            {
                month = DateTime.UtcNow.ToString("yyyy-MM");
            }

            if (!DateTime.TryParse(month + "-01", out var targetMonth))
            {
                targetMonth = DateTime.UtcNow;
            }

            var lastPeriod = profile.LastPeriodDate.Value;
            var cycleLength = profile.AvgCycleLength ?? 28;

            var targetYear = targetMonth.Year;
            var targetMon = targetMonth.Month;

            var currentPeriodStart = lastPeriod;
            DateTime bestPeriodStart = lastPeriod;
            DateTime bestOvulation = lastPeriod.AddDays(cycleLength - 14);

            for (int i = -6; i < 18; i++)
            {
                var pStart = lastPeriod.AddDays(i * cycleLength);
                var ovDay = pStart.AddDays(cycleLength - 14);
                if (ovDay.Year == targetYear && ovDay.Month == targetMon)
                {
                    bestPeriodStart = pStart;
                    bestOvulation = ovDay;
                    break;
                }
            }

            var fertileWindowDays = new List<DateTime>();
            for (var d = bestOvulation.AddDays(-5); d <= bestOvulation.AddDays(1); d = d.AddDays(1))
            {
                fertileWindowDays.Add(d);
            }

            var result = new
            {
                FertileWindowDays = fertileWindowDays,
                OvulationDay = bestOvulation,
                PeriodPrediction = bestPeriodStart.AddDays(cycleLength)
            };

            return ApiResponse<object>.SuccessResult(result);
        }

        public async Task<ApiResponse<object>> GetOvulationTodayAsync(string userId)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null || !profile.LastPeriodDate.HasValue)
            {
                return ApiResponse<object>.SuccessResult(new
                {
                    IsInFertileWindow = false,
                    DaysUntilOvulation = 0,
                    Recommendation = "Vui lòng thiết lập ngày chu kỳ cuối cùng trong hồ sơ để xem dự báo."
                });
            }

            var alerts = await _ruleEngine.EvaluateAsync(profile);
            var br01Alert = alerts.FirstOrDefault(a => a.RuleId == "BR01");

            var today = DateTime.UtcNow.Date;
            var cycleLength = profile.AvgCycleLength ?? 28;
            var lastPeriod = profile.LastPeriodDate.Value.Date;

            var cycleDay = (today - lastPeriod).Days % cycleLength;
            if (cycleDay < 0) cycleDay += cycleLength;

            var daysUntilOvulation = (cycleLength - 14) - cycleDay;
            if (daysUntilOvulation < 0) daysUntilOvulation += cycleLength;

            var isInWindow = (cycleDay >= (cycleLength - 16) && cycleDay <= (cycleLength - 12));

            var result = new
            {
                IsInFertileWindow = isInWindow,
                DaysUntilOvulation = daysUntilOvulation,
                Recommendation = br01Alert != null ? br01Alert.SuggestionVi : "Hiện tại mami chưa nằm trong cửa sổ thụ thai vàng. Hãy giữ tinh thần vui vẻ thoải mái nhé."
            };

            return ApiResponse<object>.SuccessResult(result);
        }

        public ApiResponse<List<IvfMilestone>> CreateIvfTimeline(DateTime startDate, string protocol)
        {
            var timeline = new List<IvfMilestone>();

            if (string.Equals(protocol, "long", StringComparison.OrdinalIgnoreCase))
            {
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 1,
                    Date = startDate,
                    Title = "Bắt đầu ức chế buồng trứng",
                    Description = "Bắt đầu tiêm/dùng GnRH agonist (ví dụ: Decapeptyl/Suprefact) để ngăn chặn rụng trứng tự phát."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 14,
                    Date = startDate.AddDays(13),
                    Title = "Siêu âm baseline & Kích thích buồng trứng",
                    Description = "Kiểm tra độ mỏng nội mạc tử cung. Bắt đầu tiêm hormone kích thích nang noãn (FSH/hMG)."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 24,
                    Date = startDate.AddDays(23),
                    Title = "Tiêm rụng trứng (Trigger Shot)",
                    Description = "Tiêm hCG (Ovidrel/Pregnyl) đúng giờ để kích thích trứng trưởng thành lần cuối."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 26,
                    Date = startDate.AddDays(25),
                    Title = "Chọc hút trứng (Egg Retrieval)",
                    Description = "Tiến hành lấy trứng từ buồng trứng dưới hướng dẫn siêu âm và gây mê nhẹ."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 31,
                    Date = startDate.AddDays(30),
                    Title = "Chuyển phôi (Embryo Transfer)",
                    Description = "Đưa phôi ngày 5 đã thụ tinh vào buồng tử cung của mẹ để làm tổ."
                });
            }
            else
            {
                // short / antagonist protocol
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 2,
                    Date = startDate.AddDays(1),
                    Title = "Kích thích buồng trứng",
                    Description = "Bắt đầu tiêm thuốc kích thích nang trứng FSH/hMG vào ngày thứ 2 của chu kỳ."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 7,
                    Date = startDate.AddDays(6),
                    Title = "Bắt đầu GnRH Antagonist",
                    Description = "Bổ sung thuốc đối kháng (ví dụ: Ganirelix/Cetrotide) để ngăn ngừa rụng trứng sớm trước khi chọc trứng."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 12,
                    Date = startDate.AddDays(11),
                    Title = "Tiêm rụng trứng (Trigger Shot)",
                    Description = "Tiêm mũi kích rụng trứng khi nang noãn đạt kích thước tối ưu."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 14,
                    Date = startDate.AddDays(13),
                    Title = "Chọc hút trứng (Egg Retrieval)",
                    Description = "Tiến hành thu hoạch noãn chuẩn bị thụ tinh trong phòng Lab."
                });
                timeline.Add(new IvfMilestone
                {
                    DayNumber = 19,
                    Date = startDate.AddDays(18),
                    Title = "Chuyển phôi (Embryo Transfer)",
                    Description = "Đưa phôi nuôi ngày 5 trở lại buồng tử cung để bắt đầu quá trình làm tổ."
                });
            }

            return ApiResponse<List<IvfMilestone>>.SuccessResult(timeline, "Tạo dòng thời gian IVF thành công.");
        }
    }
}
