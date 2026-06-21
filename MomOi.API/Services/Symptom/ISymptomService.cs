using MomOi.API.DTOs;
using System.Threading.Tasks;

namespace MomOi.API.Services.Symptom
{
    public class SymptomRequestDto
    {
        public string TextDescription { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? ImageMimeType { get; set; }
    }

    public interface ISymptomService
    {
        Task<ApiResponse<object>> AddSymptomEntryAsync(string userId, SymptomRequestDto request);
        Task<ApiResponse<object>> GetSymptomEntriesAsync(string userId, int? minSeverity);
        Task<ApiResponse<object>> GetSymptomEntryByIdAsync(string userId, int id);
    }
}
