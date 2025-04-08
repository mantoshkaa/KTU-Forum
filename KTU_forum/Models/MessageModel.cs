using System;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class MessageModel
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public UserModel User { get; set; } // which user sent it

        public int RoomId { get; set; }
        public RoomModel Room { get; set; } // in which room is it

        public string Content { get; set; } // content as a text message

        public string ImagePath { get; set; } // content as an image

        public DateTime SentAt { get; set; } = DateTime.UtcNow; // when was it sent

        public int Likes { get; set; } = 0;

        [CustomValidation(typeof(MessageModel), nameof(ValidateContentOrImage))]
        public object ValidationCheck => null;

        // method for making either text message or an image required to send something, since we can't put required tag on both
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
