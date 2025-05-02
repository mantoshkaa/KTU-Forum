using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using KTU_forum.Data;
using Microsoft.EntityFrameworkCore;
using KTU_forum.Models;
using System;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace KTU_forum.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;

        public ChatHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SendMessage(string username, string roomName, string message, string role = null)
        {
            try
            {
                // Find the user by username
                var user = _dbContext.Users.FirstOrDefault(u => u.Username == username);

                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                // Find the room by name
                var room = _dbContext.Rooms.FirstOrDefault(r => r.Name == roomName);

                if (room == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Room not found");
                    return;
                }

                if (string.IsNullOrEmpty(role) && user != null)
                {
                    role = user.Role;
                }

                // Create and save message to database with userId and roomId
                var newMessage = new MessageModel
                {
                    UserId = user.Id,
                    RoomId = room.Id,
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    SenderRole = role,
                    Likes = new List<LikeModel>() // Initialize empty likes collection
                };

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                // Broadcast the message to all clients in that room
                await Clients.Group(roomName).SendAsync("ReceiveMessage", username, message, user.ProfilePicturePath ?? "/profile-pictures/default.png", role);

                // Also send the new message ID back to the client
                await Clients.Caller.SendAsync("MessageSent", newMessage.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendMessage: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred sending your message");
            }
        }

        public async Task JoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task LikeMessage(int messageId, string username)
        {
            try
            {
                Console.WriteLine($"LikeMessage called with messageId: {messageId}, username: {username}");

                // Find the user by username
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                // Find the message by ID with its likes and room
                var message = await _dbContext.Messages
                    .Include(m => m.Likes)
                    .Include(m => m.Room)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                // Check if the user has already liked this message
                var existingLike = await _dbContext.Likes
                    .FirstOrDefaultAsync(l => l.MessageId == messageId && l.UserId == user.Id);

                if (existingLike != null)
                {
                    // The user has already liked this message
                    await Clients.Caller.SendAsync("ErrorMessage", "You have already liked this message");
                    return;
                }

                // Create a new like entry
                var like = new LikeModel
                {
                    MessageId = messageId,
                    UserId = user.Id
                };

                // Add the like to the database
                _dbContext.Likes.Add(like);
                await _dbContext.SaveChangesAsync();

                // Get the updated count
                int likeCount = await _dbContext.Likes.CountAsync(l => l.MessageId == messageId);

                // Broadcast the updated like count to all clients in the room
                if (message.Room != null)
                {
                    await Clients.Group(message.Room.Name).SendAsync("UpdateLikes", messageId, likeCount);
                    Console.WriteLine($"UpdateLikes sent for messageId: {messageId}, likeCount: {likeCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LikeMessage: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while liking the message");
            }
        }

        public async Task SendReply(string username, string roomName, string message, int replyToId, string role = null)
        {
            try
            {
                // Find the user by username
                var user = _dbContext.Users.FirstOrDefault(u => u.Username == username);

                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                // Find the room by name
                var room = _dbContext.Rooms.FirstOrDefault(r => r.Name == roomName);

                if (room == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Room not found");
                    return;
                }

                // Find the message being replied to
                var originalMessage = await _dbContext.Messages
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == replyToId);

                if (originalMessage == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Original message not found");
                    return;
                }

                if (string.IsNullOrEmpty(role) && user != null)
                {
                    role = user.Role;
                }

                // Create and save message to database with userId, roomId, and replyToId
                var newMessage = new MessageModel
                {
                    UserId = user.Id,
                    RoomId = room.Id,
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    SenderRole = role,
                    ReplyToId = replyToId,  // Set the reply reference
                    Likes = new List<LikeModel>()
                };

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                // Send profile pic as-is
                string profilePic = user.ProfilePicturePath;

                // Get original message info for the reply
                string originalSender = originalMessage.User.Username;
                string originalContent = originalMessage.Content;

                // Broadcast the reply message to all clients in that room
                await Clients.Group(roomName).SendAsync("ReceiveReply",
                    username, message, profilePic, role, newMessage.Id,
                    replyToId, originalSender, originalContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendReply: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred sending your reply");
            }
        }
    }
}