using Microsoft.Extensions.Options;
using MomOi.API.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MomOi.API.Services.FileStorage
{
    public sealed class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(IOptions<FileStorageOptions> options)
        {
            var configuredPath = options.Value.RootPath;
            _rootPath = Path.GetFullPath(
                Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(AppContext.BaseDirectory, configuredPath));
            Directory.CreateDirectory(_rootPath);
        }

        public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
        {
            var path = GetSafePath(storageKey);
            var directory = Path.GetDirectoryName(path);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            await using var output = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await content.CopyToAsync(output, cancellationToken);
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var path = GetSafePath(storageKey);
            if (!File.Exists(path))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            return Task.FromResult<Stream?>(stream);
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            var path = GetSafePath(storageKey);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return Task.CompletedTask;
        }

        private string GetSafePath(string storageKey)
        {
            if (string.IsNullOrWhiteSpace(storageKey) ||
                Path.IsPathRooted(storageKey) ||
                storageKey.Contains('/') ||
                storageKey.Contains('\\') ||
                storageKey.Contains("..", StringComparison.Ordinal))
            {
                throw new ArgumentException("Storage key không hợp lệ.", nameof(storageKey));
            }

            var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
            if (!path.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Storage key không hợp lệ.", nameof(storageKey));
            }

            return path;
        }
    }
}
