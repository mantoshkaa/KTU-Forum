using KTU_forum.Data;
using KTU_forum.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KTU_forum.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;

        public ChatHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SendMessage(string username, string roomName, string message, string role = null, string imagePath = null)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(roomName))
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Username and room name are required");
                    return;
                }

                // Find the user by username
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                // Find the room by name
                var room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.Name == roomName);
                if (room == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Room not found");
                    return;
                }

                // Set role if not provided
                role ??= user.Role;

                // Validate message or image
                if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(imagePath))
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message or image is required");
                    return;
                }

                // Create message
                var newMessage = new MessageModel
                {
                    UserId = user.Id,
                    RoomId = room.Id,
                    Content = message ?? "",
                    ImagePath = imagePath,
                    SentAt = DateTime.UtcNow,
                    SenderRole = role,
                    Likes = new List<LikeModel>()
                };

                // Validate message
                var validationContext = new ValidationContext(newMessage);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(newMessage, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
                    await Clients.Caller.SendAsync("ErrorMessage", errors);
                    return;
                }

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                // Broadcast the message to all clients in the room
                await Clients.Group(roomName).SendAsync(
                    "ReceiveMessage",
                    username,
                    newMessage.Content,
                    user.ProfilePicturePath ?? "/profile-pictures/default.png",
                    role,
                    newMessage.Id,
                    newMessage.ImagePath
                );

                // Send the new message ID back to the caller
                await Clients.Caller.SendAsync("MessageSent", newMessage.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred sending your message");
            }
        }

        public async Task JoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task LeaveRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task LikeMessage(int messageId, string username)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var message = await _dbContext.Messages
                    .Include(m => m.Likes)
                    .Include(m => m.Room)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                var existingLike = await _dbContext.Likes
                    .FirstOrDefaultAsync(l => l.MessageId == messageId && l.UserId == user.Id);

                if (existingLike != null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You have already liked this message");
                    return;
                }

                var like = new LikeModel
                {
                    MessageId = messageId,
                    UserId = user.Id
                };

                _dbContext.Likes.Add(like);
                await _dbContext.SaveChangesAsync();

                int likeCount = await _dbContext.Likes.CountAsync(l => l.MessageId == messageId);

                if (message.Room != null)
                {
                    await Clients.Group(message.Room.Name).SendAsync("UpdateLikeStatus", messageId, true, likeCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LikeMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while liking the message");
            }
        }

        public async Task RemoveLike(int messageId, string username)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var message = await _dbContext.Messages
                    .Include(m => m.Likes)
                    .Include(m => m.Room)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                var like = await _dbContext.Likes
                    .FirstOrDefaultAsync(l => l.MessageId == messageId && l.UserId == user.Id);

                if (like == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Like not found");
                    return;
                }

                _dbContext.Likes.Remove(like);
                await _dbContext.SaveChangesAsync();

                int likeCount = await _dbContext.Likes.CountAsync(l => l.MessageId == messageId);

                await Clients.Group(message.Room.Name).SendAsync("UpdateLikeStatus", messageId, false, likeCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveLike: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while removing the like");
            }
        }

        public async Task SendReply(string username, string roomName, string message, int replyToId, string role = null)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var room = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.Name == roomName);
                if (room == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Room not found");
                    return;
                }

                var originalMessage = await _dbContext.Messages
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == replyToId);

                if (originalMessage == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Original message not found");
                    return;
                }

                role ??= user.Role;

                var newMessage = new MessageModel
                {
                    UserId = user.Id,
                    RoomId = room.Id,
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    SenderRole = role,
                    ReplyToId = replyToId,
                    Likes = new List<LikeModel>()
                };

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                string profilePic = user.ProfilePicturePath ?? "/profile-pictures/default.png";
                string originalSender = originalMessage.User.Username;
                string originalContent = originalMessage.Content;

                await Clients.Group(roomName).SendAsync("ReceiveReply",
                    username, message, profilePic, role, newMessage.Id,
                    replyToId, originalSender, originalContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendReply: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred sending your reply");
            }
        }

        public async Task DeleteMessage(int messageId, string username)
        {
            try
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

                if (message.User.Username != username)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only delete your own messages.");
                    return;
                }

                var roomName = message.Room.Name;
                _dbContext.Likes.RemoveRange(message.Likes);
                _dbContext.Messages.Remove(message);
                await _dbContext.SaveChangesAsync();

                await Clients.Group(roomName).SendAsync("MessageDeleted", messageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while deleting the message");
            }
        }

        public async Task EditMessage(int messageId, string username, string newContent)
        {
            try
            {
                var message = await _dbContext.Messages
                    .Include(m => m.User)
                    .Include(m => m.Room)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found.");
                    return;
                }

                if (message.User.Username != username)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only edit your own messages.");
                    return;
                }

                var roomName = message.Room.Name;
                message.Content = newContent;
                message.IsEdited = true;

                await _dbContext.SaveChangesAsync();

                await Clients.Group(roomName).SendAsync("MessageEdited", messageId, newContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EditMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred while editing the message");
            }
        }

        // Private messaging methods (unchanged from your version)
        public async Task GetOrCreateConversation(string user1Username, string user2Username)
        {
            try
            {
                var user1 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == user1Username);
                var user2 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == user2Username);

                if (user1 == null || user2 == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "One or both users not found");
                    return;
                }

                var conversation = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == user1.Id && c.User2Id == user2.Id) ||
                        (c.User1Id == user2.Id && c.User2Id == user1.Id));

                if (conversation == null)
                {
                    conversation = new ConversationModel
                    {
                        User1Id = user1.Id,
                        User2Id = user2.Id,
                        CreatedAt = DateTime.UtcNow,
                        LastMessageAt = DateTime.UtcNow
                    };

                    _dbContext.Conversations.Add(conversation);
                    await _dbContext.SaveChangesAsync();

                    await Clients.Caller.SendAsync("ConversationLoaded", conversation.Id, user2Username, user2.ProfilePicturePath ?? "/profile-pictures/default.png", new List<object>());
                    return;
                }

                if (conversation.User1Id == user1.Id)
                {
                    conversation.User1LastViewedAt = DateTime.UtcNow;
                }
                else
                {
                    conversation.User2LastViewedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();

                var messages = await _dbContext.PrivateMessages
                    .Where(m => m.ConversationId == conversation.Id)
                    .OrderByDescending(m => m.SentAt)
                    .Take(50)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        Id = m.Id,
                        Content = m.Content,
                        SentAt = m.SentAt,
                        IsRead = m.IsRead,
                        SenderId = m.SenderId,
                        SenderName = m.Sender.Username,
                        SenderProfilePic = m.Sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                        IsFromCurrentUser = m.SenderId == user1.Id,
                        ReplyToId = m.ReplyToId,
                        ReplyToContent = m.ReplyTo != null ? m.ReplyTo.Content : null,
                        ReplyToSenderName = m.ReplyTo != null ? m.ReplyTo.Sender.Username : null,
                        IsEdited = m.IsEdited,
                        LikesCount = m.LikesCount
                    })
                    .ToListAsync();

                var unreadMessages = await _dbContext.PrivateMessages
                    .Where(m => m.ConversationId == conversation.Id && m.ReceiverId == user1.Id && !m.IsRead)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                await _dbContext.SaveChangesAsync();

                var otherUser = user1.Id == conversation.User1Id ? user2 : user1;

                await Clients.Caller.SendAsync("ConversationLoaded",
                    conversation.Id,
                    otherUser.Username,
                    otherUser.ProfilePicturePath ?? "/profile-pictures/default.png",
                    messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOrCreateConversation: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred loading the conversation");
            }
        }

        public async Task SendPrivateMessage(string senderUsername, string receiverUsername, string content, int? replyToId = null)
        {
            try
            {
                var sender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == senderUsername);
                var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == receiverUsername);

                if (sender == null || receiver == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "One or both users not found");
                    return;
                }

                var conversation = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == sender.Id && c.User2Id == receiver.Id) ||
                        (c.User1Id == receiver.Id && c.User2Id == sender.Id));

                if (conversation == null)
                {
                    conversation = new ConversationModel
                    {
                        User1Id = sender.Id,
                        User2Id = receiver.Id,
                        CreatedAt = DateTime.UtcNow,
                        LastMessageAt = DateTime.UtcNow
                    };

                    _dbContext.Conversations.Add(conversation);
                    await _dbContext.SaveChangesAsync();
                }

                conversation.LastMessageAt = DateTime.UtcNow;

                var privateMessage = new PrivateMessageModel
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    ConversationId = conversation.Id,
                    Content = content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    ReplyToId = replyToId
                };

                _dbContext.PrivateMessages.Add(privateMessage);
                await _dbContext.SaveChangesAsync();

                string replyToContent = null;
                string replyToSenderName = null;

                if (replyToId.HasValue)
                {
                    var replyToMessage = await _dbContext.PrivateMessages
                        .Include(m => m.Sender)
                        .FirstOrDefaultAsync(m => m.Id == replyToId.Value);

                    if (replyToMessage != null)
                    {
                        replyToContent = replyToMessage.Content;
                        replyToSenderName = replyToMessage.Sender.Username;
                    }
                }

                var messageData = new
                {
                    Id = privateMessage.Id,
                    Content = privateMessage.Content,
                    SentAt = privateMessage.SentAt,
                    IsRead = privateMessage.IsRead,
                    SenderId = privateMessage.SenderId,
                    SenderName = senderUsername,
                    SenderProfilePic = sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    ReplyToId = privateMessage.ReplyToId,
                    ReplyToContent = replyToContent,
                    ReplyToSenderName = replyToSenderName,
                    IsEdited = privateMessage.IsEdited,
                    LikesCount = privateMessage.LikesCount
                };

                await Clients.User(senderUsername).SendAsync("ReceivePrivateMessage",
                    conversation.Id,
                    receiverUsername,
                    true,
                    messageData);

                var receiverMessageData = new Dictionary<string, object>(messageData.GetType()
                    .GetProperties()
                    .ToDictionary(
                        prop => prop.Name,
                        prop => prop.GetValue(messageData)
                    ));

                receiverMessageData["IsFromCurrentUser"] = false;

                await Clients.User(receiverUsername).SendAsync("ReceivePrivateMessage",
                    conversation.Id,
                    senderUsername,
                    false,
                    receiverMessageData);

                await Clients.User(receiverUsername).SendAsync("NewPrivateMessage",
                    conversation.Id,
                    senderUsername,
                    sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendPrivateMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred sending your message");
            }
        }

        public async Task MarkPrivateMessagesAsRead(string username, int conversationId)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var conversation = await _dbContext.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Conversation not found");
                    return;
                }

                if (conversation.User1Id == user.Id)
                {
                    conversation.User1LastViewedAt = DateTime.UtcNow;
                }
                else if (conversation.User2Id == user.Id)
                {
                    conversation.User2LastViewedAt = DateTime.UtcNow;
                }
                else
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User is not part of this conversation");
                    return;
                }

                var unreadMessages = await _dbContext.PrivateMessages
                    .Where(m => m.ConversationId == conversationId && m.ReceiverId == user.Id && !m.IsRead)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                await _dbContext.SaveChangesAsync();

                var otherUserId = conversation.User1Id == user.Id ? conversation.User2Id : conversation.User1Id;
                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                if (otherUser != null)
                {
                    await Clients.User(otherUser.Username).SendAsync("PrivateMessagesRead", conversationId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkPrivateMessagesAsRead: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred marking messages as read");
            }
        }

        public async Task GetUserConversations(string username)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var conversations = await _dbContext.Conversations
                    .Where(c => c.User1Id == user.Id || c.User2Id == user.Id)
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();

                var conversationList = new List<object>();

                foreach (var conversation in conversations)
                {
                    bool isUser1 = conversation.User1Id == user.Id;
                    int otherUserId = isUser1 ? conversation.User2Id : conversation.User1Id;

                    var otherUser = await _dbContext.Users.FindAsync(otherUserId);
                    if (otherUser == null) continue;

                    var lastMessage = await _dbContext.PrivateMessages
                        .Where(m => m.ConversationId == conversation.Id)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefaultAsync();

                    var unreadCount = await _dbContext.PrivateMessages
                        .CountAsync(m =>
                            m.ConversationId == conversation.Id &&
                            m.ReceiverId == user.Id &&
                            !m.IsRead);

                    DateTime? lastViewedAt = isUser1 ? conversation.User1LastViewedAt : conversation.User2LastViewedAt;

                    conversationList.Add(new
                    {
                        ConversationId = conversation.Id,
                        Username = otherUser.Username,
                        ProfilePicture = otherUser.ProfilePicturePath ?? "/profile-pictures/default.png",
                        LastMessage = lastMessage?.Content ?? "",
                        LastMessageTime = lastMessage?.SentAt ?? conversation.CreatedAt,
                        UnreadCount = unreadCount,
                        LastViewedAt = lastViewedAt
                    });
                }

                await Clients.Caller.SendAsync("UserConversationsLoaded", conversationList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserConversations: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred loading conversations");
            }
        }

        public async Task LikePrivateMessage(int messageId, string username)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var message = await _dbContext.PrivateMessages
                    .Include(m => m.Conversation)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                message.LikesCount++;
                await _dbContext.SaveChangesAsync();

                var otherUserId = message.Conversation.User1Id == user.Id
                    ? message.Conversation.User2Id
                    : message.Conversation.User1Id;

                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                if (otherUser != null)
                {
                    await Clients.Users(new List<string> { username, otherUser.Username })
                        .SendAsync("PrivateMessageLiked", messageId, message.LikesCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LikePrivateMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred liking the message");
            }
        }

        public async Task EditPrivateMessage(int messageId, string username, string newContent)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var message = await _dbContext.PrivateMessages
                    .Include(m => m.Conversation)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                if (message.SenderId != user.Id)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only edit your own messages");
                    return;
                }

                message.Content = newContent;
                message.IsEdited = true;
                await _dbContext.SaveChangesAsync();

                var otherUserId = message.Conversation.User1Id == user.Id
                    ? message.Conversation.User2Id
                    : message.Conversation.User1Id;

                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                if (otherUser != null)
                {
                    await Clients.Users(new List<string> { username, otherUser.Username })
                        .SendAsync("PrivateMessageEdited", messageId, newContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EditPrivateMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred editing the message");
            }
        }

        public async Task DeletePrivateMessage(int messageId, string username)
        {
            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "User not found");
                    return;
                }

                var message = await _dbContext.PrivateMessages
                    .Include(m => m.Conversation)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Message not found");
                    return;
                }

                if (message.SenderId != user.Id)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only delete your own messages");
                    return;
                }

                var conversationId = message.ConversationId;
                var otherUserId = message.Conversation.User1Id == user.Id
                    ? message.Conversation.User2Id
                    : message.Conversation.User1Id;
                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                _dbContext.PrivateMessages.Remove(message);
                await _dbContext.SaveChangesAsync();

                if (otherUser != null)
                {
                    await Clients.Users(new List<string> { username, otherUser.Username })
                        .SendAsync("PrivateMessageDeleted", messageId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeletePrivateMessage: {ex.Message}\nStack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred deleting the message");
            }
        }
    }
}