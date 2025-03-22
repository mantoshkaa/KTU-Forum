using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class User
    {
        [Key]
        public string Username { get; set; }

        public string? email { get; set; }

        public string? password { get; set; }
    }
}
