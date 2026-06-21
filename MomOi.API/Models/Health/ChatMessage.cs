using System;

namespace MomOi.API.Models.Health
{
    /// <summary>
    /// Represents a single message inside a chat session between user and AI.
    /// </summary>
    public class ChatMessage
    {
        public int Id { get; set; }

        /// <summary>Foreign key to the parent ChatSession.</summary>
        public int ChatSessionId { get; set; }

        /// <summary>"user" or "bot"</summary>
        public string Sender { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ChatSession Session { get; set; } = null!;
    }

    /// <summary>
    /// A chat session grouping multiple messages for one user.
    /// Maps from MongoDB ChatHistory schema.
    /// </summary>
    public class ChatSession
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        /// <summary>Optional session identifier for multi-session support.</summary>
        public string? SessionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public System.Collections.Generic.ICollection<ChatMessage> Messages { get; set; }
            = new System.Collections.Generic.List<ChatMessage>();
    }
}
