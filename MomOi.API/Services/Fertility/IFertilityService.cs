using MomOi.API.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MomOi.API.Services.Fertility
{
    public class IvfMilestone
    {
        public int DayNumber { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public interface IFertilityService
    {
        Task<ApiResponse<object>> LogCycleAsync(string userId, DateTime periodStartDate, int cycleLength, string[] symptoms);
        Task<ApiResponse<object>> GetCalendarAsync(string userId, string month);
        Task<ApiResponse<object>> GetOvulationTodayAsync(string userId);
        ApiResponse<List<IvfMilestone>> CreateIvfTimeline(DateTime startDate, string protocol);
    }
}
