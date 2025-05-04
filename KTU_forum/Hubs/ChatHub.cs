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

                // Broadcast the updated like status to all clients in the room
                if (message.Room != null)
                {
                    await Clients.Group(message.Room.Name).SendAsync("UpdateLikeStatus", messageId, true, likeCount);
                    Console.WriteLine($"UpdateLikeStatus sent for messageId: {messageId}, hasLiked: true, likeCount: {likeCount}");
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

        public async Task RemoveLike(int messageId, string username)
{
    try
    {
        // Find the user by username first
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            await Clients.Caller.SendAsync("ErrorMessage", "User not found");
            return;
        }

        // Include the User navigation property to avoid null reference
        var message = await _dbContext.Messages
            .Include(m => m.Likes)
            .Include(m => m.Room)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
        {
            await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
            return;
        }

        // Find the like by both message ID and user ID
        var like = await _dbContext.Likes
            .FirstOrDefaultAsync(l => l.MessageId == messageId && l.UserId == user.Id);

        if (like == null)
        {
            await Clients.Caller.SendAsync("ErrorMessage", "Like not found");
            return;
        }

        // Remove the like
        _dbContext.Likes.Remove(like);
        await _dbContext.SaveChangesAsync();

        // Get the updated count
        int likeCount = await _dbContext.Likes.CountAsync(l => l.MessageId == messageId);

        // Notify all clients in the room about the like status change
        await Clients.Group(message.Room.Name).SendAsync("UpdateLikeStatus", messageId, false, likeCount);
        Console.WriteLine($"UpdateLikeStatus sent for messageId: {messageId}, hasLiked: false, likeCount: {likeCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in RemoveLike: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while removing the like");
    }
}

        public async Task DeleteMessage(int messageId, string username)
        {
            var message = await _dbContext.Messages
                .Include(m => m.User)
                .Include(m => m.Room)
                .Include(m => m.Likes)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Message not found.");
                return;
            }

            // Only allow the message owner to delete it
            if (message.User.Username != username)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "You can only delete your own messages.");
                return;
            }

            // Store room name before deleting for broadcasting
            var roomName = message.Room.Name;

            // Remove all likes for this message
            var likes = await _dbContext.Likes.Where(l => l.MessageId == messageId).ToListAsync();
            _dbContext.Likes.RemoveRange(likes);

            // Remove the message
            _dbContext.Messages.Remove(message);
            await _dbContext.SaveChangesAsync();

            // Notify all clients about the deletion
            await Clients.Group(roomName).SendAsync("MessageDeleted", messageId);

            // Log for debugging
            Console.WriteLine($"Message {messageId} deleted by {username}");
        }

        // Add this method to your ChatHub.cs file
        public async Task EditMessage(int messageId, string username, string newContent)
        {
            try
            {
                // Find the message to edit
                var message = await _dbContext.Messages
                    .Include(m => m.User)
                    .Include(m => m.Room)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found.");
                    return;
                }

                // Only allow the message owner to edit it
                if (message.User.Username != username)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only edit your own messages.");
                    return;
                }

                // Store room name for broadcasting
                var roomName = message.Room.Name;

                // Update the message content
                message.Content = newContent;
                message.IsEdited = true;  // You'll need to add this field to your MessageModel

                // Save changes
                await _dbContext.SaveChangesAsync();

                // Notify all clients about the edit
                await Clients.Group(roomName).SendAsync("MessageEdited", messageId, newContent);

                // Log for debugging
                Console.WriteLine($"Message {messageId} edited by {username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EditMessage: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while editing the message");
            }
        }
    }
}