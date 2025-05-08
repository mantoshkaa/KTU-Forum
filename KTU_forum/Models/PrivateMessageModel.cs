using System;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class PrivateMessageModel
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public UserModel Sender { get; set; }

        public int ReceiverId { get; set; }
        public UserModel Receiver { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
        public int? ReplyToId { get; set; }
        public PrivateMessageModel ReplyTo { get; set; }

        public bool IsEdited { get; set; } = false;

        // Optional: For file or image attachments
        public string AttachmentPath { get; set; }
    }
}