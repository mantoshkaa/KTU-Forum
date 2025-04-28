using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using KTU_forum.Data;      // <-- your ApplicationDbContext namespace
using Microsoft.EntityFrameworkCore;
using KTU_forum.Models;
using System;
using Microsoft.AspNetCore.Identity;

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
                    RoomId = room.Id,  // Assign the RoomId
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    SenderRole = role
                };

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                // Broadcast the message to all clients in that room
                await Clients.Group(roomName).SendAsync("ReceiveMessage", username, message, user.ProfilePicturePath ?? "/pfps/default.png", role);
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
                // Find the user by username
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                // Find the message by ID
                var message = await _dbContext.Messages
                    .Include(m => m.Likes)  // Include the Likes collection to check if the user already liked
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
                    // The user has already liked this message, you can either toggle or do nothing
                    await Clients.Caller.SendAsync("ErrorMessage", "You have already liked this message.");
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

                // Get the updated like count
                var likeCount = message.Likes.Count;

                // Broadcast the updated like count to all clients in the room
                var room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.Id == message.RoomId);
                await Clients.Group(room.Name).SendAsync("UpdateLikes", messageId, likeCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LikeMessage: {ex.Message}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while liking the message");
            }
        }




    }
}

