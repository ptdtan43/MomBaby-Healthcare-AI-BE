namespace MomOi.API.DTOs.Feedback
{
    public class CreateFeedbackDto
    {
        public string Category { get; set; } = "General";
        public string Message { get; set; } = string.Empty;
        public string? Page { get; set; }
    }
}
