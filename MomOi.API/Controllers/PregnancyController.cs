using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MomOi.API.DTOs;
using MomOi.API.Middleware;
using MomOi.API.Models.Identity;
using MomOi.API.Services.Pregnancy;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MomOi.API.Controllers
{
    /// <summary>
    /// Controller for managing pregnancy journeys, nutrition checks, weight logs, and exercise tracking.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/pregnancy")]
    public class PregnancyController : ControllerBase
    {
        private readonly IPregnancyService _pregnancyService;

        public PregnancyController(IPregnancyService pregnancyService)
        {
            _pregnancyService = pregnancyService;
        }

        public class SetupPregnancyRequest
        {
            public DateTime LastMenstrualPeriod { get; set; }
            public DateTime? DueDate { get; set; }
        }

        /// <summary>
        /// Initializes pregnancy tracker, calculating current week, trimester, and milestones.
        /// </summary>
        [HttpPost("setup")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetupPregnancy([FromBody] SetupPregnancyRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _pregnancyService.SetupPregnancyAsync(userId, request.LastMenstrualPeriod, request.DueDate);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves baby size indicators, developmental progress, and tips for the current week.
        /// </summary>
        [HttpGet("this-week")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetThisWeek()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _pregnancyService.GetThisWeekAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        public class FoodLogRequest
        {
            public string[] Foods { get; set; } = Array.Empty<string>();
        }

        /// <summary>
        /// Logs foods consumed and evaluates pregnancy safety checks. Gated under the Modern Mom subscription tier.
        /// </summary>
        [HttpPost("food-log")]
        [RequiresTier(SubscriptionTier.MomHienDai)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LogFood([FromBody] FoodLogRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _pregnancyService.LogFoodAsync(userId, request.Foods);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Generates a customized 7-day maternal meal plan. Gated under the Modern Mom subscription tier.
        /// </summary>
        [HttpGet("meal-plan")]
        [RequiresTier(SubscriptionTier.MomHienDai)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMealPlan([FromQuery] int? week)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _pregnancyService.GetMealPlanAsync(userId, week);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        public class WeightLogRequest
        {
            public float WeightKg { get; set; }
            public DateTime Date { get; set; }
        }

        /// <summary>
        /// Records maternal weight progress and checks for abnormal gain rates.
        /// </summary>
        [HttpPost("weight-log")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LogWeight([FromBody] WeightLogRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _pregnancyService.LogWeightAsync(userId, request.WeightKg, request.Date);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Retrieves recommended exercise plan matching the user's trimester. Gated under the Modern Mom subscription tier.
        /// </summary>
        [HttpGet("exercise-plan")]
        [RequiresTier(SubscriptionTier.MomHienDai)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExercisePlan()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _pregnancyService.GetExercisePlanAsync(userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        public class ExerciseLogRequest
        {
            public int StepCount { get; set; }
            public string ExerciseType { get; set; } = string.Empty;
            public int DurationMinutes { get; set; }
        }

        /// <summary>
        /// Logs step count and exercise activities, checking for physical activity deficiency (BR03).
        /// </summary>
        [HttpPost("exercise-log")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LogExercise([FromBody] ExerciseLogRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _pregnancyService.LogExerciseAsync(userId, request.StepCount, request.ExerciseType, request.DurationMinutes);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
