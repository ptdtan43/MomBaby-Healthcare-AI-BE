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
        /// Requests a WHO-compliant daily menu for a baby from the Python recommendation engine.
        /// Baby info (age, weight, allergies) is sent in the request body so the Python service
        /// does not need to read the .NET-owned baby_profiles table (schemas differ).
        /// </summary>
        public async Task<object?> GetBabyDailyMenuAsync(int ageMonths, float? weightKg, string[] allergies)
        {
            return await PostMenuRecommendAsync("/api/menu/daily", ageMonths, weightKg, allergies);
        }

        /// <summary>
        /// Requests a 7-day WHO-compliant menu for a baby from the Python recommendation engine.
        /// </summary>
        public async Task<object?> GetBabyWeeklyMenuAsync(int ageMonths, float? weightKg, string[] allergies)
        {
            return await PostMenuRecommendAsync("/api/menu/weekly", ageMonths, weightKg, allergies);
        }

        private async Task<object?> PostMenuRecommendAsync(string path, int ageMonths, float? weightKg, string[] allergies)
        {
            try
            {
                var payload = new { age_months = ageMonths, weight_kg = weightKg, allergies };
                var response = await _httpClient.PostAsJsonAsync(path, payload);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }

                _logger.LogWarning("Nutrition API {Path} returned error: {StatusCode}", path, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Nutrition API {Path}", path);
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

    }
}
