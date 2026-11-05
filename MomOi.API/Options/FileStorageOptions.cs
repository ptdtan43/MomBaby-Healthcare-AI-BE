namespace MomOi.API.Options
{
    public sealed class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        public string RootPath { get; set; } = "uploads";

        public long MaxPhotoSizeBytes { get; set; } = 5 * 1024 * 1024;
    }
}
