using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Recipe
{
    public interface IRecipeService
    {
        Task<ApiResponse<object>> GetMyRecipesAsync(string userId, bool? isSaved, int page = 1, int limit = 20);
        Task<ApiResponse<object>> GetRecipeAsync(string userId, int recipeId);
        Task<ApiResponse<object>> ToggleSaveRecipeAsync(string userId, int recipeId);
        Task<ApiResponse<object>> GetCurrentProfileAsync(string userId);
    }
}
