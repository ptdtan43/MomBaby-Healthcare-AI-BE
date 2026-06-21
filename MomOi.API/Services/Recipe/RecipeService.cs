using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MomOi.API.Services.Recipe
{
    public class RecipeService : IRecipeService
    {
        private readonly AppDbContext _context;

        public RecipeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<object>> GetMyRecipesAsync(string userId, bool? isSaved, int page = 1, int limit = 20)
        {
            var query = _context.Recipes.Where(r => r.UserId == userId);

            if (isSaved.HasValue)
            {
                query = query.Where(r => r.IsSaved == isSaved.Value);
            }

            var total = await query.CountAsync();
            var recipes = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return ApiResponse<object>.SuccessResult(new
            {
                total,
                page,
                limit,
                recipes
            }, "Tải danh sách thực đơn thành công.");
        }

        public async Task<ApiResponse<object>> GetRecipeAsync(string userId, int recipeId)
        {
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId);
            if (recipe == null) return ApiResponse<object>.FailureResult("Không tìm thấy công thức nấu ăn này.");

            return ApiResponse<object>.SuccessResult(recipe, "Lấy thông tin chi tiết món ăn thành công.");
        }

        public async Task<ApiResponse<object>> ToggleSaveRecipeAsync(string userId, int recipeId)
        {
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId);
            if (recipe == null) return ApiResponse<object>.FailureResult("Không tìm thấy công thức nấu ăn này.");

            recipe.IsSaved = !recipe.IsSaved;
            recipe.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var msg = recipe.IsSaved ? "Đã lưu món ăn vào sổ tay." : "Đã hủy lưu món ăn khỏi sổ tay.";
            return ApiResponse<object>.SuccessResult(new { isSaved = recipe.IsSaved }, msg);
        }

        public async Task<ApiResponse<object>> GetCurrentProfileAsync(string userId)
        {
            var healthProfile = await _context.MomHealthProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var stage = healthProfile?.Stage.ToString() ?? "Unknown";

            string description = stage switch
            {
                "PrePregnancy" => "Bạn đang ở giai đoạn chuẩn bị mang thai. Cơ thể bạn cần các dưỡng chất hỗ trợ thụ thai, giàu axit folic, chất chống oxy hóa và duy trì cân nặng lành mạnh.",
                "Pregnancy" => "Bạn đang trong thai kỳ. Việc bổ sung sắt, canxi, chất xơ và ăn các thực phẩm an toàn cho thai nhi là cực kỳ quan trọng để bé phát triển khỏe mạnh.",
                "Postpartum" => "Bạn đã sinh em bé. Thực đơn tập trung vào hồi phục sau sinh, lợi sữa, tăng cường omega-3 và các món ăn dễ tiêu hóa.",
                _ => "Vui lòng cập nhật hồ sơ sức khỏe để nhận được tư vấn cá nhân hóa."
            };

            return ApiResponse<object>.SuccessResult(new
            {
                profileId = stage,
                description
            }, "Tải thông tin hồ sơ dinh dưỡng hiện tại thành công.");
        }
    }
}
