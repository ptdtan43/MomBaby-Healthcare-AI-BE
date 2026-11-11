using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MomOi.API.Constants;
using MomOi.API.Data;
using MomOi.API.DTOs.DoctorCallback;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Repositories;
using MomOi.API.Services.DoctorCallback;

namespace MomOi.Tests;

public class DoctorCallbackServiceTests
{
    private static readonly CreateDoctorCallbackDto ValidRequest = new()
    {
        ContactPhone = "0912 345 678",
        Concern = "Bé 8 tháng sốt nhẹ từ tối qua, mẹ muốn hỏi cách chăm.",
        PreferredTime = "Sau 19h"
    };

    [Theory]
    [InlineData("0912345678", "0912345678")]
    [InlineData("0912.345.678", "0912345678")]
    [InlineData("+84 912 345 678", "0912345678")]
    [InlineData("0212345678", null)]
    [InlineData("12345", null)]
    [InlineData("", null)]
    public void NormalizePhoneAcceptsOnlyVietnameseMobileNumbers(string input, string? expected)
    {
        Assert.Equal(expected, DoctorCallbackService.NormalizePhone(input));
    }

    [Fact]
    public async Task CreateRejectsUsersBelowSuperMomVip()
    {
        var (service, _) = CreateService(SubscriptionTier.MomHienDai);

        var response = await service.CreateAsync("mom-1", ValidRequest);

        Assert.False(response.Success);
        Assert.Equal("TIER_REQUIRED", response.ErrorCode);
    }

    [Fact]
    public async Task CreateStoresNormalizedPhoneForVipMom()
    {
        var (service, db) = CreateService(SubscriptionTier.SuperMomVip);

        var response = await service.CreateAsync("mom-1", ValidRequest);

        Assert.True(response.Success);
        var stored = Assert.Single(db.DoctorCallbackRequests);
        Assert.Equal("0912345678", stored.ContactPhone);
        Assert.Equal(DoctorCallbackStatus.Pending, stored.Status);
        Assert.Equal(0m, stored.Fee);
    }

    [Fact]
    public async Task CreateRejectsSecondRequestWhileOneIsPending()
    {
        var (service, _) = CreateService(SubscriptionTier.SuperMomVip);
        await service.CreateAsync("mom-1", ValidRequest);

        var response = await service.CreateAsync("mom-1", ValidRequest);

        Assert.False(response.Success);
        Assert.Equal("CALLBACK_ALREADY_PENDING", response.ErrorCode);
    }

    [Fact]
    public async Task CreateRejectsWhenPreviousFeeIsUnpaid()
    {
        var (service, db) = CreateService(SubscriptionTier.SuperMomVip);
        db.DoctorCallbackRequests.Add(new DoctorCallbackRequest
        {
            UserId = "mom-1", ContactPhone = "0912345678", Concern = "Cuộc gọi trước",
            Status = DoctorCallbackStatus.Completed, Fee = DoctorCallbackPricing.FeePerCall
        });
        await db.SaveChangesAsync();

        var response = await service.CreateAsync("mom-1", ValidRequest);

        Assert.False(response.Success);
        Assert.Equal("CALLBACK_FEE_UNPAID", response.ErrorCode);
    }

    [Fact]
    public async Task CompleteChargesFeeButUnreachableDoesNot()
    {
        var (service, db) = CreateService(SubscriptionTier.SuperMomVip);
        db.DoctorCallbackRequests.AddRange(
            new DoctorCallbackRequest { UserId = "mom-1", ContactPhone = "0912345678", Concern = "Hỏi về sữa mẹ" },
            new DoctorCallbackRequest { UserId = "mom-1", ContactPhone = "0912345678", Concern = "Hỏi về giấc ngủ" });
        await db.SaveChangesAsync();
        var ids = db.DoctorCallbackRequests.Select(r => r.Id).OrderBy(i => i).ToList();

        var completed = await service.CompleteAsync("expert-1", ids[0], "Đã hướng dẫn chườm ấm");
        var unreachable = await service.MarkUnreachableAsync("expert-1", ids[1], null);

        Assert.True(completed.Success);
        Assert.Equal(DoctorCallbackPricing.FeePerCall, completed.Data!.Fee);
        Assert.True(completed.Data.FeeDue);

        Assert.True(unreachable.Success);
        Assert.Equal(0m, unreachable.Data!.Fee);
        Assert.False(unreachable.Data.FeeDue);
    }

    [Fact]
    public async Task ResolvedRequestCannotBeResolvedOrCancelledAgain()
    {
        var (service, db) = CreateService(SubscriptionTier.SuperMomVip);
        db.DoctorCallbackRequests.Add(new DoctorCallbackRequest { UserId = "mom-1", ContactPhone = "0912345678", Concern = "Hỏi về ăn dặm" });
        await db.SaveChangesAsync();
        var id = db.DoctorCallbackRequests.Single().Id;
        await service.CompleteAsync("expert-1", id, null);

        var again = await service.MarkUnreachableAsync("expert-1", id, null);
        var cancel = await service.CancelAsync("mom-1", id);

        Assert.Equal("CALLBACK_NOT_PENDING", again.ErrorCode);
        Assert.Equal("CALLBACK_NOT_PENDING", cancel.ErrorCode);
        Assert.Equal(DoctorCallbackPricing.FeePerCall, db.DoctorCallbackRequests.Single().Fee);
    }

    [Fact]
    public async Task MomCannotCancelAnotherMomsRequest()
    {
        var (service, db) = CreateService(SubscriptionTier.SuperMomVip);
        db.DoctorCallbackRequests.Add(new DoctorCallbackRequest { UserId = "mom-2", ContactPhone = "0912345678", Concern = "Hỏi về tiêm chủng" });
        await db.SaveChangesAsync();

        var response = await service.CancelAsync("mom-1", db.DoctorCallbackRequests.Single().Id);

        Assert.Equal("CALLBACK_NOT_FOUND", response.ErrorCode);
    }

    private static (DoctorCallbackService Service, AppDbContext Db) CreateService(SubscriptionTier tier)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var user = new AppUser { Id = "mom-1", FullName = "Mẹ Test", Tier = tier, TierExpiresAt = DateTime.UtcNow.AddDays(10) };
        db.Users.Add(user);
        db.Users.Add(new AppUser { Id = "mom-2", FullName = "Mẹ Khác" });
        db.SaveChanges();

        var store = new Mock<IUserStore<AppUser>>();
        var userManager = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => db.Users.FirstOrDefault(u => u.Id == id));

        var service = new DoctorCallbackService(new UnitOfWork(db), userManager.Object,
            NullLogger<DoctorCallbackService>.Instance);
        return (service, db);
    }
}
