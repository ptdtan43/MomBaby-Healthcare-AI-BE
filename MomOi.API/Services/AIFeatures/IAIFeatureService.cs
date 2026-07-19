using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.AIFeatures
{
    public class GenerateAiRecipesRequestDto
    {
        public string Query { get; set; } = string.Empty;
    }

    public interface IAIFeatureService
    {
        /// <summary>
        /// Generates recipes with Gemini AND persists them as PendingReview so the
        /// Expert can approve/reject them (recipe review workflow).
        /// </summary>
        Task<ApiResponse<object>> GenerateAIRecipesAsync(string userId, GenerateAiRecipesRequestDto request);
    }
}
