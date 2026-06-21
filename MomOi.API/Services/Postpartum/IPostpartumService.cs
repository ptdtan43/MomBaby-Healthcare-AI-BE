using MomOi.API.DTOs;
using MomOi.API.Services.AI;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Services.Postpartum
{
    public interface IPostpartumService
    {
        Task<ApiResponse<object>> SetupPostpartumAsync(string userId, DateTime deliveryDate, string deliveryType, bool isBreastfeeding);
        Task<ApiResponse<object>> SubmitEpdsAsync(string userId, int[] answers);
        Task<ApiResponse<VoiceJournalResult>> AnalyzeVoiceJournalAsync(string userId, string audioBase64, string mimeType);
        Task<ApiResponse<object>> LogBreastfeedingAsync(string userId, string side, int durationMinutes, DateTime time);
        Task<ApiResponse<object>> GetRecoveryPlanAsync(string userId, int? day);
    }
}
