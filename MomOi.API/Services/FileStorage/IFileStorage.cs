using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MomOi.API.Services.FileStorage
{
    public interface IFileStorage
    {
        Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);
        Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
        Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    }
}
