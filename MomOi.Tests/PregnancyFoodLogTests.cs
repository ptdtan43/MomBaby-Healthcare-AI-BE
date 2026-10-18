using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Repositories;
using MomOi.API.Services.BusinessRules;
using MomOi.API.Services.Pregnancy;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MomOi.Tests
{
    /// <summary>
    /// POST /api/pregnancy/food-log: lưu bữa ăn không kèm số liệu ước đoán, BR02 chỉ xét bữa vừa ghi.
    /// </summary>
    public class PregnancyFoodLogTests
    {
        [Theory]
        [InlineData(1, 0, MealType.Breakfast)]   // 08:00 VN
        [InlineData(21, 30, MealType.Breakfast)] // 04:30 VN hôm sau
        [InlineData(5, 0, MealType.Lunch)]       // 12:00 VN
        [InlineData(8, 0, MealType.Snack)]       // 15:00 VN
        [InlineData(12, 0, MealType.Dinner)]     // 19:00 VN
        [InlineData(16, 0, MealType.Snack)]      // 23:00 VN
        [InlineData(20, 0, MealType.Snack)]      // 03:00 VN
        public void InferMealType_UsesVietnamTime(int utcHour, int utcMinute, MealType expected)
        {
            var utc = new DateTime(2026, 10, 18, utcHour, utcMinute, 0, DateTimeKind.Utc);
            Assert.Equal(expected, PregnancyService.InferMealType(utc));
        }

        private static (AppDbContext Context, PregnancyService Service, Mock<IBusinessRuleEngine> Engine) CreateService()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.MomHealthProfiles.Add(new MomHealthProfile
            {
                UserId = "mom-1",
                Stage = JourneyStage.Pregnant,
                PregnancyWeek = 20
            });
            context.SaveChanges();

            var engine = new Mock<IBusinessRuleEngine>();
            // NutritionProxyService chỉ dùng cho thực đơn, LogFoodAsync không gọi tới.
            var service = new PregnancyService(new UnitOfWork(context), engine.Object, null!);
            return (context, service, engine);
        }

        [Fact]
        public async Task LogFoodAsync_SavesTrimmedFoods_NoGuessedCalories_ChecksOnlyThisMeal()
        {
            var (context, service, engine) = CreateService();

            var response = await service.LogFoodAsync("mom-1", new[] { " Cháo gà ", "", "Sữa chua" }, MealType.Lunch);

            Assert.True(response.Success);
            var meal = Assert.Single(context.MealLogs);
            Assert.Equal(new[] { "Cháo gà", "Sữa chua" }, meal.FoodItems);
            Assert.Equal(MealType.Lunch, meal.MealType);
            Assert.Equal(0f, meal.Calories);

            engine.Verify(e => e.EvaluateFoodSafetyAsync(
                It.IsAny<MomHealthProfile>(),
                It.Is<IReadOnlyCollection<string>>(f => f.SequenceEqual(new[] { "Cháo gà", "Sữa chua" }))), Times.Once);
            engine.Verify(e => e.EvaluateAsync(It.IsAny<MomHealthProfile>()), Times.Never);
        }

        [Fact]
        public async Task LogFoodAsync_OnlyBlankFoods_FailsWithoutSaving()
        {
            var (context, service, _) = CreateService();

            var response = await service.LogFoodAsync("mom-1", new[] { " ", "" });

            Assert.False(response.Success);
            Assert.Empty(context.MealLogs);
        }
    }
}
