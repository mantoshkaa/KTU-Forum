using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class User
    {
        [Key]
        public string Username { get; set; }

        public string? email { get; set; }

        public string? password { get; set; }

        public string? profilePicturePath { get; set; } // stored in Images folder until we have a database

        public string EmailVerificationToken { get; set; } // Token for email verification
        public bool EmailVerified { get; set; } // Flag to check if the email is verified
    }
}
