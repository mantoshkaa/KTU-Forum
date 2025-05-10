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

        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile imageFile, string messageContent = "")
        {
            try
            {
                _logger.LogInformation("OnPostUploadImageAsync: Received imageFile={ImageFileName}, messageContent={MessageContent}", imageFile?.FileName, messageContent);

                if (imageFile == null && string.IsNullOrWhiteSpace(messageContent))
                {
                    _logger.LogWarning("Validation failed: No image or message provided");
                    return new JsonResult(new { success = false, error = "A message or image is required" });
                }

                string imagePath = null;
                if (imageFile != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        _logger.LogWarning("Invalid file type: {Extension}", extension);
                        return new JsonResult(new { success = false, error = "Invalid file type. Use JPG, PNG, or GIF." });
                    }
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        _logger.LogWarning("File too large: {FileSize} bytes", imageFile.Length);
                        return new JsonResult(new { success = false, error = "Image too large. Maximum size is 5MB." });
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "Uploads");
                    _logger.LogInformation("Saving image to {UploadsFolder}", uploadsFolder);
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString() + extension;
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    imagePath = $"/Uploads/{fileName}";
                    _logger.LogInformation("Image saved: {ImagePath}", imagePath);
                }

                var username = HttpContext.Session.GetString("Username");
                var roomName = HttpContext.Request.Query["roomName"].ToString();
                _logger.LogInformation("Session Username={Username}, RoomName={RoomName}", username, roomName);

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogError("No username in session");
                    return new JsonResult(new { success = false, error = "User not authenticated" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Name == roomName);
                if (user == null || room == null)
                {
                    _logger.LogError("Invalid user or room: User={User}, Room={Room}", user, room);
                    return new JsonResult(new { success = false, error = "Invalid user or room" });
                }

                var newMessage = new MessageModel
                {
                    UserId = user.Id,
                    RoomId = room.Id,
                    Content = messageContent ?? "",
                    ImagePath = imagePath,
                    SentAt = DateTime.UtcNow,
                    SenderRole = user.Role,
                    Likes = new List<LikeModel>()
                };

                var validationContext = new ValidationContext(newMessage);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(newMessage, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
                    _logger.LogWarning("Message validation failed: {Errors}", errors);
                    return new JsonResult(new { success = false, error = errors });
                }

                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Message saved: Id={MessageId}", newMessage.Id);

                await _hubContext.Clients.Group(roomName).SendAsync(
                    "ReceiveMessage",
                    username,
                    newMessage.Content,
                    user.ProfilePicturePath ?? "/profile-pictures/default.png",
                    user.Role,
                    newMessage.Id,
                    newMessage.ImagePath
                );
                _logger.LogInformation("Message broadcasted to room: {RoomName}", roomName);

                return new JsonResult(new { success = true, messageId = newMessage.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostUploadImageAsync");
                return new JsonResult(new { success = false, error = "Server error occurred while processing the request" });
            }
        }
    }
}