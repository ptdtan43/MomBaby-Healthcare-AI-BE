using MomOi.API.DTOs;
using MomOi.API.DTOs.Expert;
using System.Threading.Tasks;

namespace MomOi.API.Services.Expert
{
    public interface IExpertService
    {
        /// <summary>Get all recipes with PendingReview status for Expert to review.</summary>
        Task<ApiResponse<object>> GetPendingRecipesAsync();

        /// <summary>Approve or reject a recipe by ID.</summary>
        Task<ApiResponse<object>> ReviewRecipeAsync(int recipeId, string expertId, ReviewRecipeDto dto);

        /// <summary>Get list of all Moms assigned to this expert (all Moms in current version).</summary>
        Task<ApiResponse<object>> GetAssignedMomsAsync();

        /// <summary>Send a consultation message to a Mom via chat.</summary>
        Task<ApiResponse<object>> ConsultMomAsync(string momId, string expertId, ConsultDto dto);
    }
}
