using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KTU_forum.Models
{
    public class RoomModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // e.g., "Mess Hall" and "Study"

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<MessageModel> Messages { get; set; }
    }
}
