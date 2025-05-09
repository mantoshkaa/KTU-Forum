// ConversationModel.cs
using System;
using System.Collections.Generic;

namespace KTU_forum.Models
{
    public class ConversationModel
    {
        public int Id { get; set; }

        // The two users in this one-on-one conversation
        public int User1Id { get; set; }
        public UserModel User1 { get; set; }

        public int User2Id { get; set; }
        public UserModel User2 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        // Last time User1 viewed the conversation
        public DateTime? User1LastViewedAt { get; set; }

        // Last time User2 viewed the conversation
        public DateTime? User2LastViewedAt { get; set; }

        // Navigation property for messages in this conversation
        public ICollection<PrivateMessageModel> Messages { get; set; }
    }
}