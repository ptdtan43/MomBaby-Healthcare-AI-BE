using MomOi.API.DTOs;
using MomOi.API.Services.AI;
using System.Threading.Tasks;

namespace MomOi.API.Services.AIFeatures
{
    public class AIFeatureService : IAIFeatureService
    {
        private readonly IGeminiService _geminiService;

        public AIFeatureService(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        public async Task<ApiResponse<object>> GenerateAIRecipesAsync(GenerateAiRecipesRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return ApiResponse<object>.FailureResult("Vui lòng cung cấp yêu cầu (Query).");
            }

            var aiResponseJson = await _geminiService.GenerateMultiAiDietRecipesAsync(request.Query);

            return ApiResponse<object>.SuccessResult(new { recipesJson = aiResponseJson }, "Tạo công thức món ăn AI thành công.");
        }
    }
}
