using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Integration
{
    public interface IUsdaClientService
    {
        /// <summary>
        /// Fetches foods from USDA API matching the query and saves them to the local database.
        /// </summary>
        /// <param name="query">Food search term (e.g. "Apple")</param>
        /// <param name="maxItems">Number of items to fetch</param>
        /// <returns>ApiResponse indicating how many items were synced.</returns>
        Task<ApiResponse<object>> SyncFoodsAsync(string query, int maxItems);
    }
}
