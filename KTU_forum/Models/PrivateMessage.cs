// PrivateMessageModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class PrivateMessageModel
    {
        public int Id { get; set; }

        // The user who sent the message
        public int SenderId { get; set; }
        public UserModel Sender { get; set; }

        // The user who received the message
        public int ReceiverId { get; set; }
        public UserModel Receiver { get; set; }

        // The conversation this message belongs to
        public int ConversationId { get; set; }
        public ConversationModel Conversation { get; set; }

        [MaxLength(500)]
        public string Content { get; set; }

        [MaxLength(255)]
        public string ImagePath { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsEdited { get; set; } = false;

        // Self-referencing property for replies
        public int? ReplyToId { get; set; }
        public PrivateMessageModel ReplyTo { get; set; }

        // Simple likes tracking
        public int LikesCount { get; set; } = 0;

        // Custom validation to ensure either content or image is provided
        [CustomValidation(typeof(PrivateMessageModel), nameof(ValidateContentOrImage))]
        public object ValidationCheck => null;

        public static ValidationResult ValidateContentOrImage(object _, ValidationContext context)
        {
            var message = (PrivateMessageModel)context.ObjectInstance;

            if (string.IsNullOrWhiteSpace(message.Content) && string.IsNullOrWhiteSpace(message.ImagePath))
            {
                return new ValidationResult("A message must contain either text or an image.");
            }

            return ValidationResult.Success;
        }
    }
}