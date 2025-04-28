using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class MessageModel
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public UserModel User { get; set; } // Which user sent it

        public int RoomId { get; set; }
        public RoomModel Room { get; set; } // In which room was it sent

        [MaxLength(500)] // Optional: restrict message content length (adjust as needed)
        public string Content { get; set; } // Text message content

        [MaxLength(255)] // Optional: limit length of image path
        public string ImagePath { get; set; } // Path for an image attached to the message

        public DateTime SentAt { get; set; } = DateTime.UtcNow; // When was it sent
        public string SenderRole { get; set; }

        public ICollection<LikeModel> Likes { get; set; }
        public int LikesCount => Likes?.Count ?? 0;  // Using the count of the Likes collection

        // Custom validation to ensure either content or image is provided
        [CustomValidation(typeof(MessageModel), nameof(ValidateContentOrImage))]
        public object ValidationCheck => null;

        // Validation method to ensure either content or image is provided
        public static ValidationResult ValidateContentOrImage(object _, ValidationContext context)
        {
            var message = (MessageModel)context.ObjectInstance;

            if (string.IsNullOrWhiteSpace(message.Content) && string.IsNullOrWhiteSpace(message.ImagePath))
            {
                return new ValidationResult("A message must contain either text or an image.");
            }

            return ValidationResult.Success;
        }
    }
}
