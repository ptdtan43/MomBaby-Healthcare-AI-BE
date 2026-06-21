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
        Task<ApiResponse<object>> GenerateAIRecipesAsync(GenerateAiRecipesRequestDto request);
    }
}
