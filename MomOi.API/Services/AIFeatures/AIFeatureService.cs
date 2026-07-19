using Microsoft.Extensions.Logging;
using MomOi.API.DTOs;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RecipeEntity = MomOi.API.Models.Health.Recipe;

namespace MomOi.API.Services.AIFeatures
{
    public class AIFeatureService : IAIFeatureService
    {
        private readonly IGeminiService _geminiService;
        private readonly IGenericRepository<RecipeEntity> _recipeRepo;
        private readonly IGenericRepository<MomHealthProfile> _profileRepo;
        private readonly ILogger<AIFeatureService> _logger;

        public AIFeatureService(
            IGeminiService geminiService,
            IGenericRepository<RecipeEntity> recipeRepo,
            IGenericRepository<MomHealthProfile> profileRepo,
            ILogger<AIFeatureService> logger)
        {
            _geminiService = geminiService;
            _recipeRepo = recipeRepo;
            _profileRepo = profileRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> GenerateAIRecipesAsync(string userId, GenerateAiRecipesRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return ApiResponse<object>.FailureResult("Vui lòng cung cấp yêu cầu (Query).");
            }

            string aiResponseJson;
            JsonElement recipesArray;
            try
            {
                aiResponseJson = await _geminiService.GenerateMultiAiDietRecipesAsync(request.Query);
                using var doc = JsonDocument.Parse(aiResponseJson);
                recipesArray = doc.RootElement.Clone();
                if (recipesArray.ValueKind != JsonValueKind.Array)
                {
                    return ApiResponse<object>.FailureResult("AI trả về dữ liệu không đúng định dạng. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI recipe generation failed for user {UserId}.", userId);
                return ApiResponse<object>.FailureResult("AI hiện không thể tạo công thức. Vui lòng thử lại sau.");
            }

            // Persist every generated recipe as PendingReview so the Expert portal picks it up
            // (recipe review workflow: AI generates -> PendingReview -> Expert approves/rejects).
            var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UserId == userId);
            var profileStage = profile?.Stage.ToString() ?? "unknown";

            var savedCount = 0;
            foreach (var item in recipesArray.EnumerateArray())
            {
                var title = (item.TryGetProperty("recipe", out var titleEl) || item.TryGetProperty("Recipe", out titleEl) || item.TryGetProperty("title", out titleEl) || item.TryGetProperty("Title", out titleEl)) 
                    ? titleEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(title)) continue;

                var calories = (item.TryGetProperty("calories", out var calEl) || item.TryGetProperty("Calories", out calEl)) && calEl.ValueKind == JsonValueKind.Number
                    ? calEl.GetInt32() : 0;

                var ingredients = new List<string>();
                if ((item.TryGetProperty("ingredients", out var ingEl) || item.TryGetProperty("Ingredients", out ingEl)) && ingEl.ValueKind == JsonValueKind.Array)
                    foreach (var i in ingEl.EnumerateArray()) ingredients.Add(i.GetString() ?? "");

                var steps = new List<string>();
                if ((item.TryGetProperty("steps", out var stepEl) || item.TryGetProperty("Steps", out stepEl)) && stepEl.ValueKind == JsonValueKind.Array)
                    foreach (var s in stepEl.EnumerateArray()) steps.Add(s.GetString() ?? "");

                await _recipeRepo.AddAsync(new RecipeEntity
                {
                    UserId = userId,
                    ProfileStage = profileStage,
                    Title = title,
                    Description = $"Gợi ý bởi AI theo yêu cầu: {request.Query}",
                    IngredientsJson = JsonSerializer.Serialize(ingredients),
                    StepsJson = JsonSerializer.Serialize(steps),
                    Calories = calories,
                    Status = RecipeStatus.PendingReview,
                    GeneratedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                savedCount++;
            }

            if (savedCount > 0)
            {
                await _recipeRepo.SaveChangesAsync();
            }

            return ApiResponse<object>.SuccessResult(
                new { recipesJson = aiResponseJson, pendingCount = savedCount },
                $"Đã tạo {savedCount} công thức và gửi cho chuyên gia xét duyệt.");
        }
    }
}
