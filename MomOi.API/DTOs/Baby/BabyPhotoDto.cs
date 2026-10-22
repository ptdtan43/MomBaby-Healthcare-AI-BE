using System;

namespace MomOi.API.DTOs.Baby
{
    public sealed class BabyPhotoDto
    {
        public bool HasPhoto { get; init; }
        public string ContentType { get; init; } = string.Empty;
        public DateTime? UpdatedAt { get; init; }
    }
}
