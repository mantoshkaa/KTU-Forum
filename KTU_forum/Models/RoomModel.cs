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
        public List<MessageModel> Messages { get; set; } = new(); // one room can have many messages, one message can be allocated to one room
    }
}
