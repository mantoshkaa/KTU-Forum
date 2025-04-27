using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using KTU_forum.Data;      // <-- your ApplicationDbContext namespace
using Microsoft.EntityFrameworkCore;
using KTU_forum.Models;
using System;

namespace KTU_forum.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;

        public ChatHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SendMessage(string username, string roomName, string message)
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

                // Create and save message to database with userId and roomId
                var newMessage = new MessageModel
                {
                    UserId = user.Id,
                    RoomId = room.Id,  // Assign the RoomId
                    Content = message,
                    SentAt = DateTime.UtcNow
                };

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                // Broadcast the message to all clients in that room
                await Clients.Group(roomName).SendAsync("ReceiveMessage", username, message, user.ProfilePicturePath ?? "/pfps/default.png");
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


    }
}

