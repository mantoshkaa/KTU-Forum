using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using KTU_forum.Data;
using KTU_forum.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Hubs
{
    public class PrivateMessageHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public PrivateMessageHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // Connect to user's private channel
        public async Task JoinPrivateChannel(string username)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{username}");
        }

        // Send a private message
        public async Task SendPrivateMessage(string senderUsername, string receiverUsername, string message)
        {
            try
            {
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == senderUsername);
                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Username == receiverUsername);

                if (sender == null || receiver == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                // Find or create conversation
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == sender.Id && c.User2Id == receiver.Id) ||
                        (c.User1Id == receiver.Id && c.User2Id == sender.Id));

                if (conversation == null)
                {
                    conversation = new ConversationModel
                    {
                        User1Id = sender.Id,
                        User2Id = receiver.Id,
                        LastMessageAt = DateTime.UtcNow
                    };
                    _context.Conversations.Add(conversation);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    conversation.LastMessageAt = DateTime.UtcNow;
                    _context.Conversations.Update(conversation);
                }

                // Create the message
                var privateMessage = new PrivateMessageModel
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.PrivateMessages.Add(privateMessage);
                await _context.SaveChangesAsync();

                // Send to sender (for UI update)
                await Clients.Caller.SendAsync("ReceivePrivateMessage",
                    privateMessage.Id,
                    senderUsername,
                    receiverUsername,
                    message,
                    privateMessage.SentAt.ToString("yyyy-MM-dd HH:mm"),
                    sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    true); // isSender = true

                // Send to receiver if online
                await Clients.Group($"user_{receiverUsername}").SendAsync("ReceivePrivateMessage",
                    privateMessage.Id,
                    senderUsername,
                    receiverUsername,
                    message,
                    privateMessage.SentAt.ToString("yyyy-MM-dd HH:mm"),
                    sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    false); // isSender = false

                // Notify receiver about new message (for notification)
                await Clients.Group($"user_{receiverUsername}").SendAsync("NewPrivateMessageNotification",
                    senderUsername,
                    privateMessage.Id);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Error sending message: " + ex.Message);
            }
        }

        // Mark message as read
        public async Task MarkMessageAsRead(int messageId, string username)
        {
            var message = await _context.PrivateMessages.FindAsync(messageId);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (message != null && user != null && message.ReceiverId == user.Id)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();

                // Notify sender that message was read
                var sender = await _context.Users.FindAsync(message.SenderId);
                if (sender != null)
                {
                    await Clients.Group($"user_{sender.Username}").SendAsync("MessageRead", messageId);
                }
            }
        }

        // Edit a private message
        public async Task EditPrivateMessage(int messageId, string username, string newContent)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var message = await _context.PrivateMessages.FindAsync(messageId);
                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                // Check permission - only sender can edit
                if (message.SenderId != user.Id)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only edit your own messages");
                    return;
                }

                // Update message
                message.Content = newContent;
                message.IsEdited = true;
                await _context.SaveChangesAsync();

                // Get receiver
                var receiver = await _context.Users.FindAsync(message.ReceiverId);

                // Notify both sender and receiver
                await Clients.Group($"user_{username}").SendAsync("MessageEdited", messageId, newContent);
                await Clients.Group($"user_{receiver.Username}").SendAsync("MessageEdited", messageId, newContent);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Error editing message: " + ex.Message);
            }
        }

        // Delete a private message
        public async Task DeletePrivateMessage(int messageId, string username)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var message = await _context.PrivateMessages.FindAsync(messageId);
                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                // Check permission - only sender can delete
                if (message.SenderId != user.Id)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only delete your own messages");
                    return;
                }

                // Get receiver before deleting
                var receiver = await _context.Users.FindAsync(message.ReceiverId);

                // Delete message
                _context.PrivateMessages.Remove(message);
                await _context.SaveChangesAsync();

                // Notify both sender and receiver
                await Clients.Group($"user_{username}").SendAsync("MessageDeleted", messageId);
                await Clients.Group($"user_{receiver.Username}").SendAsync("MessageDeleted", messageId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Error deleting message: " + ex.Message);
            }
        }

        // Like a private message
        public async Task LikePrivateMessage(int messageId, string username)
        {
            // Similar implementation to your existing message like functionality
            // You'll need to add a PrivateMessageLike model and DbSet
        }

        // Send a reply to a private message
        public async Task SendPrivateMessageReply(string senderUsername, string receiverUsername, string message, int replyToId)
        {
            try
            {
                // Similar to SendPrivateMessage but with replyToId
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == senderUsername);
                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Username == receiverUsername);
                var replyToMessage = await _context.PrivateMessages.Include(m => m.Sender).FirstOrDefaultAsync(m => m.Id == replyToId);

                if (sender == null || receiver == null || replyToMessage == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User or reply message not found");
                    return;
                }

                // Create new message with reply reference
                var privateMessage = new PrivateMessageModel
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    ReplyToId = replyToId
                };

                _context.PrivateMessages.Add(privateMessage);
                await _context.SaveChangesAsync();

                // Send to both users
                await Clients.Group($"user_{senderUsername}").SendAsync("ReceivePrivateMessageReply",
                    privateMessage.Id,
                    senderUsername,
                    receiverUsername,
                    message,
                    privateMessage.SentAt.ToString("yyyy-MM-dd HH:mm"),
                    sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    replyToMessage.Content,
                    replyToMessage.Sender.Username,
                    true); // isSender = true

                await Clients.Group($"user_{receiverUsername}").SendAsync("ReceivePrivateMessageReply",
                    privateMessage.Id,
                    senderUsername,
                    receiverUsername,
                    message,
                    privateMessage.SentAt.ToString("yyyy-MM-dd HH:mm"),
                    sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    replyToMessage.Content,
                    replyToMessage.Sender.Username,
                    false); // isSender = false
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Error sending reply: " + ex.Message);
            }
        }
    }
}