using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MomOi.API.DTOs;
using MomOi.API.Models.Nutrition;
using MomOi.API.Repositories;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly ILogger<UsdaClientService> _logger;

        public UsdaClientService(
            HttpClient httpClient,
            IUnitOfWork unitOfWork,
            IConfiguration config,
            ILogger<UsdaClientService> logger)
        {
            _httpClient = httpClient;
            _unitOfWork = unitOfWork;
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

                    float calories = 0, protein = 0, carbs = 0, fat = 0;

                    if (food.TryGetProperty("foodNutrients", out var nutrients))
                    {
                        foreach (var nutrient in nutrients.EnumerateArray())
                        {
                            int nutrientId = nutrient.GetProperty("nutrientId").GetInt32();
                            float value = nutrient.TryGetProperty("value", out var v) ? v.GetSingle() : 0;

                            switch (nutrientId)
                            {
                                case 1008: calories = value; break;
                                case 1003: protein = value; break;
                                case 1005: carbs = value; break;
                                case 1004: fat = value; break;
                            }
                        }
                    }

                    var existing = await _unitOfWork.Repository<UsdaFoodItem>()
                        .FirstOrDefaultAsync(f => f.FdcId == fdcId);

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
                        await _unitOfWork.Repository<UsdaFoodItem>().AddAsync(new UsdaFoodItem
                        {
                            FdcId = fdcId,
                            Description = description,
                            Calories = calories,
                            Protein = protein,
                            Carbs = carbs,
                            Fat = fat,
                            SyncDate = DateTime.UtcNow
                        });
                    }
                    syncedCount++;
                }

                await _unitOfWork.SaveChangesAsync();
                return ApiResponse<object>.SuccessResult((object)$"Đã đồng bộ thành công {syncedCount} thực phẩm.", "Đồng bộ USDA thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi exception khi gọi USDA API.");
                return ApiResponse<object>.FailureResult($"Đã xảy ra lỗi: {ex.Message}");
            }
        }
    }
}
