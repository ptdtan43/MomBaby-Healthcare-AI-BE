using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MomOi.API.Models.Health;
using MomOi.API.Options;
using MomOi.API.Repositories;
using MomOi.API.Services.Baby;
using MomOi.API.Services.BusinessRules;
using MomOi.API.Services.FileStorage;
using MomOi.API.Services.Nutrition;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;

namespace MomOi.Tests;

public class BabyPhotoServiceTests
{
    [Fact]
    public async Task UploadRejectsUnsupportedContent()
    {
        var baby = new BabyProfile { Id = 1, UserId = "user-1" };
        var (service, storage) = CreateService(baby);
        var file = CreateFile(new byte[] { 1, 2, 3 }, "image/jpeg");

        var response = await service.UploadBabyPhotoAsync("user-1", 1, file);

        Assert.False(response.Success);
        Assert.Equal("PHOTO_TYPE_NOT_SUPPORTED", response.ErrorCode);
        storage.Verify(s => s.SaveAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadRejectsFilesOverConfiguredLimit()
    {
        var baby = new BabyProfile { Id = 1, UserId = "user-1" };
        var (service, storage) = CreateService(baby, maxSize: 2);
        var file = CreateFile(new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg");

        var response = await service.UploadBabyPhotoAsync("user-1", 1, file);

        Assert.False(response.Success);
        Assert.Equal("PHOTO_TOO_LARGE", response.ErrorCode);
        storage.Verify(s => s.SaveAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadRejectsBabyOwnedByAnotherUser()
    {
        var (service, storage) = CreateService(null);
        var file = CreateFile(new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg");

        var response = await service.UploadBabyPhotoAsync("other-user", 1, file);

        Assert.False(response.Success);
        Assert.Equal("BABY_NOT_FOUND", response.ErrorCode);
        storage.Verify(s => s.SaveAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadStoresValidJpegAndReturnsMetadata()
    {
        var baby = new BabyProfile { Id = 1, UserId = "user-1" };
        var (service, storage) = CreateService(baby);
        var file = CreateFile(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }, "application/octet-stream");

        var response = await service.UploadBabyPhotoAsync("user-1", 1, file);

        Assert.True(response.Success);
        Assert.True(response.Data!.HasPhoto);
        Assert.Equal("image/jpeg", response.Data.ContentType);
        Assert.Equal("image/jpeg", baby.PhotoContentType);
        storage.Verify(s => s.SaveAsync(
            It.Is<string>(key => key.StartsWith("baby-photo-")),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (BabyService Service, Mock<IFileStorage> Storage) CreateService(
        BabyProfile? baby,
        long maxSize = 5 * 1024 * 1024)
    {
        var babyRepo = new Mock<IGenericRepository<BabyProfile>>();
        babyRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<BabyProfile, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(baby);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Repository<BabyProfile>()).Returns(babyRepo.Object);
        var storage = new Mock<IFileStorage>();
        storage.Setup(s => s.SaveAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var nutrition = new NutritionProxyService(
            new HttpClient(),
            new ConfigurationBuilder().Build(),
            NullLogger<NutritionProxyService>.Instance);

        var service = new BabyService(
            unitOfWork.Object,
            Mock.Of<IBusinessRuleEngine>(),
            nutrition,
            storage.Object,
            Options.Create(new FileStorageOptions { MaxPhotoSizeBytes = maxSize }));

        return (service, storage);
    }

    private static IFormFile CreateFile(byte[] content, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", "photo.bin")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
