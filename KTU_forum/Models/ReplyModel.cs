using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KTU_forum.Models
{
    public class ReplyModel
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // foreign key to the User who posted the reply
        public int UserId { get; set; }
        public UserModel User { get; set; }

        // foreign key to the Post this reply belongs to
        public int PostId { get; set; }
        public PostModel Post { get; set; }

        // self-referencing foreign key (for nested replies)
        public int? ParentReplyId { get; set; }
        public ReplyModel ParentReply { get; set; }

        public List<ReplyModel> ChildrenReplies { get; set; } = new();
    }
}
