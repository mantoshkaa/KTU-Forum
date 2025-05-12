using KTU_forum.Data;
using KTU_forum.Hubs;
using KTU_forum.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KTU_forum.Pages
{
    public class MessHallModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessHallModel> _logger;

        public string RoomName { get; set; }
        public string Username { get; set; }
        public string ProfilePicturePath { get; set; }
        public string UserRole { get; set; }

        public class MessageViewModel
        {
            public int MessageId { get; set; }
            public string Content { get; set; }
            public DateTime SentAt { get; set; }
            public string SenderUsername { get; set; }
            public string SenderProfilePic { get; set; }
            public string SenderRole { get; set; }
            public string ImagePath { get; set; }
            public int LikesCount { get; set; }
            public bool IsReply { get; set; }
            public int? ReplyToId { get; set; }
            public string ReplyToUsername { get; set; }
            public string ReplyToContent { get; set; }
            public bool hasLiked { get; set; }
            public bool IsEdited { get; set; }
        }

        public List<MessageViewModel> Messages { get; set; }

        public MessHallModel(ApplicationDbContext context, IWebHostEnvironment environment, IHubContext<ChatHub> hubContext, ILogger<MessHallModel> logger)
        {
            _context = context;
            _environment = environment;
            _hubContext = hubContext;
            _logger = logger;
        }

        public void OnGet()
        {
            RoomName = HttpContext.Request.Query["roomName"];
            Username = HttpContext.Session.GetString("Username");
            _logger.LogInformation("OnGet: Username={Username}, RoomName={RoomName}", Username, RoomName);

            if (!string.IsNullOrEmpty(Username))
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == Username);
                if (user != null)
                {
                    ProfilePicturePath = user.ProfilePicturePath ?? "/profile-pictures/default.png";
                    UserRole = user.Role;
                }
                else
                {
                    _logger.LogWarning("User not found for Username={Username}", Username);
                    ProfilePicturePath = "/profile-pictures/default.png";
                }
            }
            else
            {
                _logger.LogWarning("No Username in session");
                ProfilePicturePath = "/profile-pictures/default.png";
            }

            var messHallRoom = _context.Rooms.FirstOrDefault(r => r.Name == RoomName);
            if (messHallRoom == null)
            {
                _logger.LogInformation("Creating new room: {RoomName}", RoomName);
                var newRoom = new RoomModel
                {
                    Name = RoomName,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Rooms.Add(newRoom);
                _context.SaveChanges();
            }

            Messages = _context.Messages
                .Where(m => m.Room.Name == RoomName)
                .Include(m => m.User)
                .Include(m => m.Likes)
                .Include(m => m.ReplyTo)
                    .ThenInclude(r => r.User)
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
                    ImagePath = m.ImagePath,
                    LikesCount = m.Likes.Count,
                    IsReply = m.ReplyToId.HasValue,
                    ReplyToId = m.ReplyToId,
                    ReplyToUsername = m.ReplyTo != null ? m.ReplyTo.User.Username : null,
                    ReplyToContent = m.ReplyTo != null ? m.ReplyTo.Content : null,
                    hasLiked = m.Likes.Any(l => l.User.Username == Username),
                    IsEdited = m.IsEdited
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostUploadImageAsync()
        {
            try
            {
                _logger.LogInformation("Processing upload request");

                // Ensure request is multipart/form-data
                if (!Request.HasFormContentType)
                {
                    _logger.LogWarning("Invalid request format: Expected multipart/form-data");
                    return new JsonResult(new { success = false, error = "Invalid request format" }) { ContentType = "application/json" };
                }

                var form = await Request.ReadFormAsync(); // Read form data
                var messageContent = form["messageContent"]; // Extract message field
                var imageFile = form.Files["imageFile"]; // Extract image file

                _logger.LogInformation("Extracted messageContent={MessageContent}, imageFile={ImageFileName}", messageContent, imageFile?.FileName);

                // Ensure either a message or an image is present
                if (imageFile == null && string.IsNullOrWhiteSpace(messageContent))
                {
                    _logger.LogWarning("Validation failed: No image or message provided");
                    return new JsonResult(new { success = false, error = "A message or image is required" }) { ContentType = "application/json" };
                }

                // Image handling...
                string imagePath = null;
                if (imageFile != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return new JsonResult(new { success = false, error = "Invalid file type" }) { ContentType = "application/json" };
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "Uploads");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    imagePath = $"/Uploads/{fileName}";
                }

                // Return a JSON response (even though form-data was used in request)
                return new JsonResult(new { success = true, messageContent, imagePath }) { ContentType = "application/json" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                return new JsonResult(new { success = false, error = "Server error" }) { ContentType = "application/json" };
            }
        }
    }
}