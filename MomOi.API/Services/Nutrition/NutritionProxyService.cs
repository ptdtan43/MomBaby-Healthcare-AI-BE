using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MomOi.API.Services.Nutrition
{
    /// <summary>
    /// Service to call the Python FastAPI nutrition service to retrieve food nutrition facts.
    /// </summary>
    public class NutritionProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NutritionProxyService> _logger;

        public NutritionProxyService(HttpClient httpClient, IConfiguration configuration, ILogger<NutritionProxyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Configure BaseAddress from settings
            var baseUrl = configuration["NutritionApiUrl"] ?? "http://localhost:8001";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        /// <summary>
        /// Contacts FastAPI USDA engine to analyze a food item query.
        /// </summary>
        /// <param name="query">The name of the food or meal query (e.g. "1 apple").</param>
        /// <returns>A dynamic JSON element representing nutrition details, or null if failed.</returns>
        public async Task<object?> AnalyzeFoodAsync(string query)
        {
            try
            {
                _logger.LogInformation("Calling Nutrition API to analyze food: {Query}", query);

                // Assuming FastAPI nutrition-api exposing GET /api/nutrition/analyze?query=...
                var response = await _httpClient.GetAsync($"/api/nutrition/analyze?query={Uri.EscapeDataString(query)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<object>();
                    return data;
                }

                _logger.LogWarning("Nutrition API returned error: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Nutrition API");
                return null;
            }
        }

        /// <summary>
        /// Retrieves a 7-day maternal meal plan for the given week from the Python nutrition engine.
        /// </summary>
        public async Task<object?> GetMealPlanAsync(int pregnancyWeek)
        {
            try
            {
                _logger.LogInformation("Calling Nutrition API to get meal plan for week {Week}", pregnancyWeek);
                var response = await _httpClient.GetAsync($"/api/nutrition/meal-plan?week={pregnancyWeek}");
                
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<object>();
                    return data;
                }

                _logger.LogWarning("Nutrition API returned error: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Nutrition API for meal plan");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the recommended daily menu for a baby from the Python nutrition engine.
        /// </summary>
        public async Task<object?> GetBabyDailyMenuAsync(int babyId)
        {
            try
            {
                _logger.LogInformation("Calling Nutrition API to get daily menu for baby {BabyId}", babyId);
                var response = await _httpClient.GetAsync($"/api/baby/{babyId}/menu/daily");
                
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<object>();
                    return data;
                }

                _logger.LogWarning("Nutrition API returned error for daily menu: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Nutrition API for daily menu of baby {BabyId}", babyId);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the recommended weekly menu for a baby from the Python nutrition engine.
        /// </summary>
        public async Task<object?> GetBabyWeeklyMenuAsync(int babyId)
        {
            try
            {
                _logger.LogInformation("Calling Nutrition API to get weekly menu for baby {BabyId}", babyId);
                var response = await _httpClient.GetAsync($"/api/baby/{babyId}/menu/weekly");
                
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<object>();
                    return data;
                }

                _logger.LogWarning("Nutrition API returned error for weekly menu: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Nutrition API for weekly menu of baby {BabyId}", babyId);
                return null;
            }
        }
    }
}
