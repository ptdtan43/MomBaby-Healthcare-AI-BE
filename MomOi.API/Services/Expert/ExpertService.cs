using Microsoft.AspNetCore.Identity;
using MomOi.API.DTOs;
using MomOi.API.DTOs.Expert;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Expert
{
    public class ExpertService : IExpertService
    {
        private readonly IUnitOfWork _unitOfWork;
        // UserManager là đặc thù của ASP.NET Identity, không thể thay bằng Repository
        private readonly UserManager<AppUser> _userManager;

        public ExpertService(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ─── Recipe Review ───────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetPendingRecipesAsync()
        {
            var allRecipes = await _unitOfWork.Repository<MomOi.API.Models.Health.Recipe>()
                .FindAsync(r => r.Status == RecipeStatus.PendingReview);

            var pending = allRecipes.OrderBy(r => r.GeneratedAt)
                .Select(r => new
                {
                    r.Id, r.Title, r.Description, r.ProfileStage, r.Category,
                    r.Calories, r.Protein, r.Carbs, r.Fat,
                    r.PrepTimeMinutes, r.Difficulty, r.Tags,
                    r.IngredientsJson, r.StepsJson, r.Status, r.GeneratedAt
                })
                .ToList();

            return ApiResponse<object>.SuccessResult(pending,
                $"Có {pending.Count} công thức đang chờ xét duyệt.");
        }

        /// <summary>
        /// Returns ALL recipes (pending, approved, rejected) with category info.
        /// Expert dashboard uses this to show two tables: Mom recipes and Baby recipes.
        /// Approved recipes appear first (with checkmark), then pending, then rejected.
        /// </summary>
        public async Task<ApiResponse<object>> GetAllRecipesAsync()
        {
            var allRecipes = await _unitOfWork.Repository<MomOi.API.Models.Health.Recipe>()
                .FindAsync(r => r.Status != RecipeStatus.Rejected);

            var result = allRecipes
                .OrderByDescending(r => r.Status == RecipeStatus.Approved)
                .ThenBy(r => r.GeneratedAt)
                .Select(r => new
                {
                    r.Id, r.Title, r.Description, r.ProfileStage,
                    r.Category,
                    r.Calories, r.Protein, r.Carbs, r.Fat,
                    r.PrepTimeMinutes, r.Difficulty, r.Tags,
                    r.IngredientsJson, r.StepsJson,
                    r.Status,
                    r.ExpertNote, r.ReviewedAt, r.GeneratedAt
                })
                .ToList()
                .Select(r => new
                {
                    r.Id, r.Title, r.Description, r.ProfileStage,
                    Category = r.Category.ToString(),
                    r.Calories, r.Protein, r.Carbs, r.Fat,
                    r.PrepTimeMinutes, r.Difficulty, r.Tags,
                    r.IngredientsJson, r.StepsJson,
                    Status = r.Status.ToString(),
                    r.ExpertNote, r.ReviewedAt, r.GeneratedAt
                })
                .ToList();

            var momRecipes = result.Where(r => r.Category == "Mom").ToList();
            var babyRecipes = result.Where(r => r.Category == "Baby").ToList();

            return ApiResponse<object>.SuccessResult(
                new { momRecipes, babyRecipes },
                $"Tổng {result.Count} công thức ({momRecipes.Count} cho mẹ, {babyRecipes.Count} cho bé).");
        }

        public async Task<ApiResponse<object>> ReviewRecipeAsync(int recipeId, string expertId, ReviewRecipeDto dto)
        {
            var recipe = await _unitOfWork.Repository<MomOi.API.Models.Health.Recipe>()
                .FirstOrDefaultAsync(r => r.Id == recipeId);
            if (recipe == null)
                return ApiResponse<object>.FailureResult("Không tìm thấy công thức.");

            if (recipe.Status != RecipeStatus.PendingReview)
                return ApiResponse<object>.FailureResult("Công thức này đã được xét duyệt trước đó.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.Note))
                return ApiResponse<object>.FailureResult("Vui lòng cung cấp lý do từ chối (Note) khi reject công thức.");

            recipe.Status = dto.IsApproved ? RecipeStatus.Approved : RecipeStatus.Rejected;
            recipe.ExpertNote = dto.Note;
            recipe.ReviewedByExpertId = expertId;
            recipe.ReviewedAt = DateTime.UtcNow;
            recipe.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            var action = dto.IsApproved ? "Phê duyệt" : "Từ chối";
            return ApiResponse<object>.SuccessResult(
                (object)new { recipe.Id, recipe.Status, recipe.ExpertNote, recipe.ReviewedAt },
                $"{action} công thức thành công.");
        }

        // ─── Mom Consultation ─────────────────────────────────────────────────────

        public async Task<ApiResponse<object>> GetAssignedMomsAsync()
        {
            var momRole = await _userManager.GetUsersInRoleAsync(AppRoles.Mom);

            var result = momRole.Select(u => new
            {
                u.Id, u.Email, u.FullName, u.CreatedAt, u.Tier
            }).OrderByDescending(u => u.CreatedAt);

            return ApiResponse<object>.SuccessResult(result, "Lấy danh sách Mẹ thành công.");
        }

        public async Task<ApiResponse<object>> ConsultMomAsync(string momId, string expertId, ConsultDto dto)
        {
            var mom = await _userManager.FindByIdAsync(momId);
            if (mom == null)
                return ApiResponse<object>.FailureResult("Không tìm thấy người dùng.");

            var sessionKey = $"expert_{expertId}";
            var session = await _unitOfWork.Repository<ChatSession>()
                .FirstOrDefaultAsync(s => s.UserId == momId && s.SessionId == sessionKey);

            if (session == null)
            {
                session = new ChatSession
                {
                    UserId = momId,
                    SessionId = sessionKey,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<ChatSession>().AddAsync(session);
                await _unitOfWork.SaveChangesAsync();
            }

            var message = new ChatMessage
            {
                ChatSessionId = session.Id,
                Sender = SenderType.Expert,
                Text = dto.Message,
                Timestamp = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ChatMessage>().AddAsync(message);

            session.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.SuccessResult(
                (object)new { MessageId = message.Id, SentAt = message.Timestamp },
                "Gửi tin nhắn tư vấn thành công.");
        }
    }
}
