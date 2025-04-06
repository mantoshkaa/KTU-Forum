using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KTU_forum.Models;

namespace KTU_forum.Models
{
    public class PostModel
    {
        public int Id { get; set; }//Primary key

        [Required]
        [StringLength(100, ErrorMessage = "Title is too long.")]
        public string Title { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Foreign Key: which user created this post
        public int UserId { get; set; }
        public UserModel User { get; set; }
    }
}