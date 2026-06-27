using MomOi.API.Models.Identity;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Represents a single message inside a chat session between user and AI.
    /// </summary>
    public class ChatMessage : BaseEntity
    {
        /// <summary>Foreign key to the parent ChatSession.</summary>
        public int ChatSessionId { get; set; }

        /// <summary>User, Bot, or Expert</summary>
        public SenderType Sender { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ChatSession Session { get; set; } = null!;
    }

    /// <summary>
    /// A chat session grouping multiple messages for one user.
    /// Maps from MongoDB ChatHistory schema.
    /// </summary>
    public class ChatSession : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        /// <summary>Optional session identifier for multi-session support.</summary>
        public string? SessionId { get; set; }

        // Navigation property
        public System.Collections.Generic.ICollection<ChatMessage> Messages { get; set; }
            = new System.Collections.Generic.List<ChatMessage>();
    }
}
