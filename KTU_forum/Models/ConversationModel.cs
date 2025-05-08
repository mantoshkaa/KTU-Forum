using System;
using System.Collections.Generic;

namespace KTU_forum.Models
{
    public class ConversationModel
    {
        public int Id { get; set; }

        // The two participants in the conversation
        public int User1Id { get; set; }
        public UserModel User1 { get; set; }

        public int User2Id { get; set; }
        public UserModel User2 { get; set; }

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        // Navigation property to messages in this conversation
        public List<PrivateMessageModel> Messages { get; set; } = new();
    }
}