using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class MessageModel
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public UserModel User { get; set; }

        public int RoomId { get; set; }
        public RoomModel Room { get; set; }

        [MaxLength(500)]
        public string Content { get; set; }

        [MaxLength(255)]
        public string ImagePath { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public string SenderRole { get; set; }

        public ICollection<LikeModel> Likes { get; set; }
        public int LikesCount => Likes?.Count ?? 0;

        public bool IsEdited { get; set; } = false;

        public int? ReplyToId { get; set; }
        public MessageModel ReplyTo { get; set; }

        [CustomValidation(typeof(MessageModel), nameof(ValidateContentOrImage))]
        public object ValidationCheck => null;

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