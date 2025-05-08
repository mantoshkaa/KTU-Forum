using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Data;
using KTU_forum.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Pages
{
    public class PrivateMessagesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PrivateMessagesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Handler to get all conversations for a user
        public async Task<IActionResult> OnGetConversationsAsync()
        {
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return new JsonResult(new { error = "Not logged in" }) { StatusCode = 401 };

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return new JsonResult(new { error = "User not found" }) { StatusCode = 404 };

            var conversations = await _context.Conversations
                .Where(c => c.User1Id == user.Id || c.User2Id == user.Id)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var conversationViewModels = new List<object>();

            foreach (var conversation in conversations)
            {
                // Determine other participant
                var otherUser = conversation.User1Id == user.Id ? conversation.User2 : conversation.User1;

                // Get last message
                var lastMessage = await _context.PrivateMessages
                    .Where(m =>
                        ((m.SenderId == conversation.User1Id && m.ReceiverId == conversation.User2Id) ||
                         (m.SenderId == conversation.User2Id && m.ReceiverId == conversation.User1Id)))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                // Count unread messages
                var unreadCount = await _context.PrivateMessages
                    .CountAsync(m => m.ReceiverId == user.Id &&
                                    m.SenderId == otherUser.Id &&
                                    !m.IsRead);

                conversationViewModels.Add(new
                {
                    conversationId = conversation.Id,
                    otherUserId = otherUser.Id,
                    otherUsername = otherUser.Username,
                    otherUserProfilePic = otherUser.ProfilePicturePath ?? "/profile-pictures/default.png",
                    lastMessageContent = lastMessage?.Content ?? "",
                    lastMessageTime = lastMessage?.SentAt.ToString("yyyy-MM-dd HH:mm") ?? "",
                    unreadCount = unreadCount
                });
            }

            return new JsonResult(conversationViewModels);
        }

        // Handler to get messages for a specific conversation
        public async Task<IActionResult> OnGetMessagesAsync(int conversationId)
        {
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return new JsonResult(new { error = "Not logged in" }) { StatusCode = 401 };

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return new JsonResult(new { error = "User not found" }) { StatusCode = 404 };

            var conversation = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return new JsonResult(new { error = "Conversation not found" }) { StatusCode = 404 };

            // Security check - ensure user is part of this conversation
            if (conversation.User1Id != user.Id && conversation.User2Id != user.Id)
                return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 403 };

            // Get the other user
            var otherUser = conversation.User1Id == user.Id ? conversation.User2 : conversation.User1;

            // Get messages
            var messages = await _context.PrivateMessages
                .Where(m =>
                    ((m.SenderId == conversation.User1Id && m.ReceiverId == conversation.User2Id) ||
                     (m.SenderId == conversation.User2Id && m.ReceiverId == conversation.User1Id)))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    senderUsername = m.Sender.Username,
                    senderProfilePic = m.Sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    time = m.SentAt.ToString("yyyy-MM-dd HH:mm"),
                    isRead = m.IsRead,
                    isSentByCurrentUser = m.SenderId == user.Id
                })
                .ToListAsync();

            // Mark unread messages as read
            var unreadMessages = await _context.PrivateMessages
                .Where(m => m.ReceiverId == user.Id && m.SenderId == otherUser.Id && !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                conversationId = conversation.Id,
                otherUser = new
                {
                    id = otherUser.Id,
                    username = otherUser.Username,
                    profilePic = otherUser.ProfilePicturePath ?? "/profile-pictures/default.png"
                },
                messages = messages
            });
        }

        // Handler to search for users
        // Handler to search for users
        public async Task<IActionResult> OnGetSearchUsersAsync(string searchTerm)
        {
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return new JsonResult(new { error = "Not logged in" }) { StatusCode = 401 };

            if (string.IsNullOrEmpty(searchTerm))
                return new JsonResult(new List<object>());

            // Make the search more flexible - search for usernames that start with the search term
            // Case insensitive search
            var users = await _context.Users
                .Where(u => u.Username.ToLower().StartsWith(searchTerm.ToLower()) && u.Username != username)
                .Take(15)  // Increased from 10 to 15
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    profilePic = u.ProfilePicturePath ?? "/profile-pictures/default.png"
                })
                .ToListAsync();

            // If no exact match was found, try a contains search
            if (users.Count == 0)
            {
                users = await _context.Users
                    .Where(u => u.Username.ToLower().Contains(searchTerm.ToLower()) && u.Username != username)
                    .Take(15)
                    .Select(u => new
                    {
                        id = u.Id,
                        username = u.Username,
                        profilePic = u.ProfilePicturePath ?? "/profile-pictures/default.png"
                    })
                    .ToListAsync();
            }

            return new JsonResult(users);
        }

        // Handler to start a new conversation
        public async Task<IActionResult> OnPostStartConversationAsync([FromBody] StartConversationModel model)
        {
            // Make sure we're capturing the model correctly
            if (model == null)
            {
                return new JsonResult(new { error = "Invalid request data" }) { StatusCode = 400 };
            }

            string currentUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(currentUsername))
            {
                return new JsonResult(new { error = "Not logged in" }) { StatusCode = 401 };
            }

            // Log the usernames for debugging
            Console.WriteLine($"Starting conversation between {currentUsername} and {model.OtherUsername}");

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
            var otherUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.OtherUsername);

            if (currentUser == null || otherUser == null)
            {
                return new JsonResult(new { error = "One or both users not found" }) { StatusCode = 404 };
            }

            // Check if conversation already exists
            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    (c.User1Id == currentUser.Id && c.User2Id == otherUser.Id) ||
                    (c.User1Id == otherUser.Id && c.User2Id == currentUser.Id));

            if (existingConversation != null)
            {
                return new JsonResult(new { conversationId = existingConversation.Id });
            }

            // Create new conversation
            var conversation = new ConversationModel
            {
                User1Id = currentUser.Id,
                User2Id = otherUser.Id,
                LastMessageAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return new JsonResult(new { conversationId = conversation.Id });
        }
    }

    public class StartConversationModel
    {
        public string OtherUsername { get; set; }
    }
}