using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using MomOi.API.Options;
using MomOi.API.Repositories;
using MomOi.API.Services.Wellness;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace MomOi.Tests
{
    /// <summary>
    /// Link nhạc của GET /api/wellness/relax-tracks: cấu hình ngoài → file wwwroot/audio → null.
    /// </summary>
    public class WellnessServiceTests : IDisposable
    {
        private const string RequestBase = "http://internal-host";
        private readonly string _webRoot;

        public WellnessServiceTests()
        {
            _webRoot = Path.Combine(Path.GetTempPath(), "momoi-wellness-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_webRoot, WellnessService.AudioFolder));
        }

        public void Dispose() => Directory.Delete(_webRoot, recursive: true);

        private void AddAudioFile(string id) =>
            File.WriteAllBytes(Path.Combine(_webRoot, WellnessService.AudioFolder, $"{id}.mp3"), new byte[] { 1 });

        private Dictionary<string, string?> GetAudioUrls(RelaxAudioOptions options)
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.Setup(e => e.WebRootFileProvider).Returns(new PhysicalFileProvider(_webRoot));

            var service = new WellnessService(
                Mock.Of<IUnitOfWork>(),
                Microsoft.Extensions.Options.Options.Create(options),
                environment.Object);

            var response = service.GetRelaxTracks(RequestBase);
            Assert.True(response.Success);

            using var json = JsonDocument.Parse(JsonSerializer.Serialize(response.Data));
            return json.RootElement.GetProperty("tracks").EnumerateArray().ToDictionary(
                t => t.GetProperty("id").GetString()!,
                t => t.GetProperty("audioUrl").ValueKind == JsonValueKind.Null
                    ? null
                    : t.GetProperty("audioUrl").GetString());
        }

        [Fact]
        public void NoFilesAndNoConfig_AllAudioUrlsAreNull()
        {
            var urls = GetAudioUrls(new RelaxAudioOptions());

            Assert.Equal(3, urls.Count);
            Assert.All(urls.Values, Assert.Null);
        }

        [Fact]
        public void FileInWebRoot_UsesPublicBaseUrl()
        {
            AddAudioFile("rain-piano");

            var urls = GetAudioUrls(new RelaxAudioOptions { PublicBaseUrl = "https://api.momoi.vn/" });

            Assert.Equal("https://api.momoi.vn/audio/rain-piano.mp3", urls["rain-piano"]);
            Assert.Null(urls["piano-lullaby"]);
        }

        [Fact]
        public void FileInWebRoot_WithoutPublicBaseUrl_FallsBackToRequestBase()
        {
            AddAudioFile("piano-lullaby");

            var urls = GetAudioUrls(new RelaxAudioOptions());

            Assert.Equal($"{RequestBase}/audio/piano-lullaby.mp3", urls["piano-lullaby"]);
        }

        [Fact]
        public void ConfiguredTrackUrl_WinsOverLocalFile()
        {
            AddAudioFile("lofi-mom-care");
            var options = new RelaxAudioOptions
            {
                PublicBaseUrl = "https://api.momoi.vn",
                Tracks = { ["lofi-mom-care"] = "https://cdn.example.com/lofi.mp3" },
            };

            var urls = GetAudioUrls(options);

            Assert.Equal("https://cdn.example.com/lofi.mp3", urls["lofi-mom-care"]);
        }
    }
}
