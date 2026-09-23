using Microsoft.Extensions.Logging;
using MomOi.API.DTOs;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RecipeEntity = MomOi.API.Models.Health.Recipe;

namespace MomOi.API.Services.AIFeatures
{
    public class AIFeatureService : IAIFeatureService
    {
        private readonly IGeminiService _geminiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AIFeatureService> _logger;

        public AIFeatureService(
            IGeminiService geminiService,
            IUnitOfWork unitOfWork,
            ILogger<AIFeatureService> logger)
        {
            _geminiService = geminiService;
            _unitOfWork = unitOfWork;
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
                
                // AI trả về object {"error": true, "message": "..."} khi đầu vào không hợp lệ
                if (recipesArray.ValueKind == JsonValueKind.Object)
                {
                    if (recipesArray.TryGetProperty("error", out var errorProp) && errorProp.GetBoolean())
                    {
                        var errorMsg = recipesArray.TryGetProperty("message", out var msgProp) 
                            ? msgProp.GetString() 
                            : "Đầu vào không hợp lệ. Vui lòng nhập tên thực phẩm thật.";
                        return ApiResponse<object>.FailureResult(errorMsg);
                    }
                    return ApiResponse<object>.FailureResult("AI trả về dữ liệu không đúng định dạng. Vui lòng thử lại.");
                }
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
            // Chỉ đọc Stage để gắn nhãn, không sửa profile => bỏ change tracking cho nhẹ.
            var profile = await _unitOfWork.Repository<MomHealthProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId, asNoTracking: true);
            var profileStage = profile?.Stage.ToString() ?? "unknown";

            var recipeRepo = _unitOfWork.Repository<RecipeEntity>();
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

                // AI trả chỉ số dạng chuỗi ("20g", "20 phút", "Dễ") hoặc số — chuẩn hóa về
                // kiểu của entity. Chấp nhận cả camelCase lẫn PascalCase.
                bool TryGet(string lower, string upper, out JsonElement el) =>
                    item.TryGetProperty(lower, out el) || item.TryGetProperty(upper, out el);

                // Lấy số từ "20g" / "20 phút" / 20  -> 20
                float Num(string lower, string upper)
                {
                    if (!TryGet(lower, upper, out var el)) return 0f;
                    if (el.ValueKind == JsonValueKind.Number) return el.GetSingle();
                    if (el.ValueKind == JsonValueKind.String)
                    {
                        var digits = new string((el.GetString() ?? "")
                            .TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
                            .Replace(',', '.');
                        if (float.TryParse(digits, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var f))
                            return f;
                    }
                    return 0f;
                }

                var difficulty = Difficulty.Easy;
                if (TryGet("difficulty", "Difficulty", out var diffEl) && diffEl.ValueKind == JsonValueKind.String)
                {
                    var d = (diffEl.GetString() ?? "").Trim().ToLowerInvariant();
                    difficulty = d switch
                    {
                        "trung bình" or "medium" => Difficulty.Medium,
                        "khó" or "hard" => Difficulty.Hard,
                        _ => Difficulty.Easy
                    };
                }

                await recipeRepo.AddAsync(new RecipeEntity
                {
                    UserId = userId,
                    ProfileStage = profileStage,
                    Title = title,
                    Description = $"Gợi ý bởi AI theo yêu cầu: {request.Query}",
                    IngredientsJson = JsonSerializer.Serialize(ingredients),
                    StepsJson = JsonSerializer.Serialize(steps),
                    Calories = calories,
                    Protein = Num("protein", "Protein"),
                    Carbs = Num("carbs", "Carbs"),
                    Fat = Num("fat", "Fat"),
                    PrepTimeMinutes = (int)Num("prepTime", "PrepTime"),
                    Difficulty = difficulty,
                    Status = RecipeStatus.PendingReview,
                    GeneratedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                savedCount++;
            }

            // Một lần commit duy nhất cho cả lô công thức: hoặc lưu được hết, hoặc không cái nào.
            if (savedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return ApiResponse<object>.SuccessResult(
                new { recipesJson = aiResponseJson, pendingCount = savedCount },
                $"Đã tạo {savedCount} công thức và gửi cho chuyên gia xét duyệt.");
        }
    }
}
