using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KTU_forum.Data;
using KTU_forum.Models;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Pages
{
    public class MessHallModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public string RoomName { get; set; }
        public string Username { get; set; }
        public string ProfilePicturePath { get; set; }

        // Updated to include both message and user info
        public class MessageViewModel
        {
            public int MessageId { get; set; }
            public string Content { get; set; }
            public DateTime SentAt { get; set; }
            public string SenderUsername { get; set; }
            public string SenderProfilePic { get; set; }
        }

        public List<MessageViewModel> Messages { get; set; }

        public MessHallModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            RoomName = HttpContext.Request.Query["roomName"];
            Username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(Username))
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == Username);
                if (user != null)
                {
                    ProfilePicturePath = user.ProfilePicturePath ?? "/pfps/default.png";
                }
                else
                {
                    ProfilePicturePath = "/pfps/default.png";
                }
            }
            else
            {
                ProfilePicturePath = "/pfps/default.png";
            }

            var messHallRoom = _context.Rooms.FirstOrDefault(r => r.Name == RoomName);

            // If the room doesn't exist, create and save it
            if (messHallRoom == null)
            {
                var newRoom = new RoomModel
                {
                    Name = RoomName,
                    CreatedAt = DateTime.UtcNow // Use UTC time to avoid timezone issues
                };

                _context.Rooms.Add(newRoom);
                _context.SaveChanges();
            }

            // Load messages with user information using a join
            Messages = _context.Messages
                .Include(m => m.User)
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageViewModel
                {
                    MessageId = m.Id,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    SenderUsername = m.User.Username,
                    SenderProfilePic = m.User.ProfilePicturePath ?? "/pfps/default.png"
                })
                .ToList();
        }
    }
}