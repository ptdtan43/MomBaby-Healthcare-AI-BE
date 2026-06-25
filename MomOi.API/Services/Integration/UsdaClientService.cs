using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MomOi.API.Data;
using MomOi.API.DTOs;
using MomOi.API.Models.Nutrition;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Integration
{
    public class UsdaClientService : IUsdaClientService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<UsdaClientService> _logger;

        public UsdaClientService(
            HttpClient httpClient,
            AppDbContext context,
            IConfiguration config,
            ILogger<UsdaClientService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> SyncFoodsAsync(string query, int maxItems)
        {
            var apiKey = _config["UsdaApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ApiResponse<object>.FailureResult("Thiếu UsdaApiKey trong cấu hình hệ thống.");
            }

            try
            {
                var url = $"https://api.nal.usda.gov/fdc/v1/foods/search?api_key={apiKey}&query={Uri.EscapeDataString(query)}&pageSize={maxItems}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("USDA API trả về lỗi: {Status}", response.StatusCode);
                    return ApiResponse<object>.FailureResult($"Lỗi khi gọi USDA API: {response.ReasonPhrase}");
                }

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (!root.TryGetProperty("foods", out var foodsElement))
                {
                    return ApiResponse<object>.FailureResult("Không tìm thấy kết quả phù hợp từ USDA.");
                }

                int syncedCount = 0;
                foreach (var food in foodsElement.EnumerateArray())
                {
                    int fdcId = food.GetProperty("fdcId").GetInt32();
                    string description = food.GetProperty("description").GetString() ?? "";

                    // Default nutrients
                    float calories = 0, protein = 0, carbs = 0, fat = 0;

                    if (food.TryGetProperty("foodNutrients", out var nutrients))
                    {
                        foreach (var nutrient in nutrients.EnumerateArray())
                        {
                            int nutrientId = nutrient.GetProperty("nutrientId").GetInt32();
                            float value = nutrient.TryGetProperty("value", out var v) ? v.GetSingle() : 0;

                            switch (nutrientId)
                            {
                                case 1008: calories = value; break; // Energy
                                case 1003: protein = value; break;  // Protein
                                case 1005: carbs = value; break;    // Carbohydrate
                                case 1004: fat = value; break;      // Total lipid (fat)
                            }
                        }
                    }

                    // Insert or update in DB
                    var existing = await _context.UsdaFoodItems.FirstOrDefaultAsync(f => f.FdcId == fdcId);
                    if (existing != null)
                    {
                        existing.Description = description;
                        existing.Calories = calories;
                        existing.Protein = protein;
                        existing.Carbs = carbs;
                        existing.Fat = fat;
                        existing.SyncDate = DateTime.UtcNow;
                    }
                    else
                    {
                        var newItem = new UsdaFoodItem
                        {
                            FdcId = fdcId,
                            Description = description,
                            Calories = calories,
                            Protein = protein,
                            Carbs = carbs,
                            Fat = fat,
                            SyncDate = DateTime.UtcNow
                        };
                        _context.UsdaFoodItems.Add(newItem);
                    }
                    syncedCount++;
                }

                await _context.SaveChangesAsync();
                return ApiResponse<object>.SuccessResult((object)$"Đã đồng bộ thành công {syncedCount} thực phẩm.", $"Đồng bộ USDA thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi exception khi gọi USDA API.");
                return ApiResponse<object>.FailureResult($"Đã xảy ra lỗi: {ex.Message}");
            }
        }
    }
}
