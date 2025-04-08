using KTU_forum.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class UserModel
    {
        public int Id { get; set; } //primary key
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [EmailDomain("ktu.lt")] //only allow this domain
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Role { get; set; }

        public string ProfilePicturePath { get; set; }

        public string EmailVerificationToken { get; set; } // for verifying email
        public bool IsVerified { get; set; }
        public string PasswordResetToken { get; set; } // for resetting pasword via email
        public DateTime? PasswordResetTokenExpiry { get; set; }


        //define relationships
        public List<PostModel> Posts { get; set; } = new(); // One user -> many posts
        public List<ReplyModel> Replies { get; set; } = new(); // One user -> many replies
        public List<MessageModel> Messages { get; set; } = new(); // One user -> many messages


    }
}