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
        public string UserRole { get; set; }
        // Updated to include both message and user info
        public class MessageViewModel
        {
            public int MessageId { get; set; }
            public string Content { get; set; }
            public DateTime SentAt { get; set; }
            public string SenderUsername { get; set; }
            public string SenderProfilePic { get; set; }
            public string SenderRole { get; set; }
            public int LikesCount { get; set; }

            // Reply functionality
            public bool IsReply { get; set; }
            public int? ReplyToId { get; set; }
            public string ReplyToUsername { get; set; }
            public string ReplyToContent { get; set; }
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
                var userr = _context.Users.FirstOrDefault(u => u.Username == Username);
                if (userr != null)
                {
                    ProfilePicturePath = userr.ProfilePicturePath ?? "/profile-pictures/default.png";
                    UserRole = userr.Role; // Make sure to set the user role
                }
                else
                {
                    ProfilePicturePath = "/profile-pictures/default.png";
                }
            }
            else
            {
                ProfilePicturePath = "/profile-pictures/default.png";
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

            Messages = _context.Messages
                .Where(m => m.Room.Name == RoomName) // Only messages for this room
                .Include(m => m.User)
                .Include(m => m.Likes)
                .Include(m => m.ReplyTo)
                    .ThenInclude(r => r.User) // Include the user of the message being replied to
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageViewModel
                {
                    MessageId = m.Id,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    SenderUsername = m.User.Username,
                    SenderProfilePic = m.User.ProfilePicturePath,
                    SenderRole = m.User.Role,
                    LikesCount = m.Likes.Count,

                    // Reply information
                    IsReply = m.ReplyToId.HasValue,
                    ReplyToId = m.ReplyToId,
                    ReplyToUsername = m.ReplyTo.User.Username,
                    ReplyToContent = m.ReplyTo.Content
                })
                .ToList();
        }
    }
}