using System;

namespace KTU_forum.Models
{
    public class UserModel
    {
        public int Id { get; set; } //primary key
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
