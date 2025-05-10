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

        public async Task SendMessage(string username, string roomName, string message, string role = null, string imagePath = null)
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

                var newMessage = new MessageModel
                {
                    UserId = user.Id,
                    RoomId = room.Id,
                };

                if (imagePath == null)
                {
                    // Create and save message to database with userId and roomId
                    newMessage = new MessageModel
                    {
                        UserId = user.Id,
                        RoomId = room.Id,
                        Content = message,
                        SentAt = DateTime.UtcNow,
                        SenderRole = role,
                        Likes = new List<LikeModel>() // Initialize empty likes collection
                    };
                }
                else
                {
                    // Create and save message to database with userId and roomId and image
                    newMessage = new MessageModel
                    {
                        UserId = user.Id,
                        RoomId = room.Id,
                        Content = message,
                        SentAt = DateTime.UtcNow,
                        SenderRole = role,
                        ImagePath = imagePath,
                        Likes = new List<LikeModel>() // Initialize empty likes collection
                    };
                }
                

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                // Broadcast the message to all clients in that room
                await Clients.Group(roomName).SendAsync("ReceiveMessage", username, message, user.ProfilePicturePath ?? "/profile-pictures/default.png", role, imagePath);

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

        // Get or create a conversation between two users
        public async Task GetOrCreateConversation(string user1Username, string user2Username)
        {
            try
            {
                // Find both users
                var user1 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == user1Username);
                var user2 = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == user2Username);

                if (user1 == null || user2 == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "One or both users not found");
                    return;
                }

                // Check if a conversation already exists between these users
                var conversation = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c =>
                        (c.User1Id == user1.Id && c.User2Id == user2.Id) ||
                        (c.User1Id == user2.Id && c.User2Id == user1.Id));

                // If no conversation exists, create a new one
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

                    // Return the new empty conversation
                    await Clients.Caller.SendAsync("ConversationLoaded", conversation.Id, user2Username, user2.ProfilePicturePath ?? "/profile-pictures/default.png", new List<object>());
                    return;
                }

                // Update the last viewed timestamp for the requesting user
                if (conversation.User1Id == user1.Id)
                {
                    conversation.User1LastViewedAt = DateTime.UtcNow;
                }
                else
                {
                    conversation.User2LastViewedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();

                // Get recent messages for this conversation
                var messages = await _dbContext.PrivateMessages
                    .Where(m => m.ConversationId == conversation.Id)
                    .OrderByDescending(m => m.SentAt)
                    .Take(50) // Load last 50 messages
                    .OrderBy(m => m.SentAt) // Display in chronological order
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

                // Mark all unread messages as read
                var unreadMessages = await _dbContext.PrivateMessages
                    .Where(m => m.ConversationId == conversation.Id && m.ReceiverId == user1.Id && !m.IsRead)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                await _dbContext.SaveChangesAsync();

                // Get the other user's info
                var otherUser = user1.Id == conversation.User1Id ? user2 : user1;

                // Send the conversation data to the caller
                await Clients.Caller.SendAsync("ConversationLoaded",
                    conversation.Id,
                    otherUser.Username,
                    otherUser.ProfilePicturePath ?? "/profile-pictures/default.png",
                    messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOrCreateConversation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred loading the conversation");
            }
        }

        // Send a private message
        public async Task SendPrivateMessage(string senderUsername, string receiverUsername, string content, int? replyToId = null)
        {
            try
            {
                // Find both users
                var sender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == senderUsername);
                var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == receiverUsername);

                if (sender == null || receiver == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "One or both users not found");
                    return;
                }

                // Get or create conversation
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

                // Update the last message time
                conversation.LastMessageAt = DateTime.UtcNow;

                // Create the private message
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

                // Gather reply information if this is a reply
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

                // Create a message object to send to clients
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

                // Send to both the sender and receiver
                // For sender, mark IsFromCurrentUser as true
                await Clients.User(senderUsername).SendAsync("ReceivePrivateMessage",
                    conversation.Id,
                    receiverUsername,
                    true, // Is from current user
                    messageData);

                // For receiver, mark IsFromCurrentUser as false
                var receiverMessageData = new Dictionary<string, object>(messageData.GetType()
                    .GetProperties()
                    .ToDictionary(
                        prop => prop.Name,
                        prop => prop.GetValue(messageData)
                    ));

                // Add "isFromCurrentUser" : false to the dictionary
                receiverMessageData["IsFromCurrentUser"] = false;

                await Clients.User(receiverUsername).SendAsync("ReceivePrivateMessage",
                    conversation.Id,
                    senderUsername,
                    false, // Not from current user 
                    receiverMessageData);

                // Send notification to receiver about new message
                await Clients.User(receiverUsername).SendAsync("NewPrivateMessage",
                    conversation.Id,
                    senderUsername,
                    sender.ProfilePicturePath ?? "/profile-pictures/default.png",
                    content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendPrivateMessage: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred sending your message");
            }
        }

        // Mark messages as read
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

                // Find the conversation
                var conversation = await _dbContext.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Conversation not found");
                    return;
                }

                // Update the last viewed timestamp
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

                // Mark all messages from the other user as read
                var unreadMessages = await _dbContext.PrivateMessages
                    .Where(m => m.ConversationId == conversationId && m.ReceiverId == user.Id && !m.IsRead)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                await _dbContext.SaveChangesAsync();

                // Get the other user
                var otherUserId = conversation.User1Id == user.Id ? conversation.User2Id : conversation.User1Id;
                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                if (otherUser != null)
                {
                    // Notify the other user that their messages have been read
                    await Clients.User(otherUser.Username).SendAsync("PrivateMessagesRead", conversationId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkPrivateMessagesAsRead: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred marking messages as read");
            }
        }

        // Get user conversations list
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

                // Get all conversations for this user
                var conversations = await _dbContext.Conversations
                    .Where(c => c.User1Id == user.Id || c.User2Id == user.Id)
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();

                var conversationList = new List<object>();

                foreach (var conversation in conversations)
                {
                    // Determine which user is the other party
                    bool isUser1 = conversation.User1Id == user.Id;
                    int otherUserId = isUser1 ? conversation.User2Id : conversation.User1Id;

                    // Get the other user's details
                    var otherUser = await _dbContext.Users.FindAsync(otherUserId);
                    if (otherUser == null) continue;

                    // Get the last message in this conversation
                    var lastMessage = await _dbContext.PrivateMessages
                        .Where(m => m.ConversationId == conversation.Id)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefaultAsync();

                    // Count unread messages
                    var unreadCount = await _dbContext.PrivateMessages
                        .CountAsync(m =>
                            m.ConversationId == conversation.Id &&
                            m.ReceiverId == user.Id &&
                            !m.IsRead);

                    // Determine last viewed time
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
                Console.WriteLine($"Error in GetUserConversations: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred loading conversations");
            }
        }

        // Like a private message
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

                // Simple implementation just increments the like count
                // This doesn't prevent multiple likes from same user
                message.LikesCount++;
                await _dbContext.SaveChangesAsync();

                // Get the other user in the conversation
                var otherUserId = message.Conversation.User1Id == user.Id
                    ? message.Conversation.User2Id
                    : message.Conversation.User1Id;

                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                if (otherUser != null)
                {
                    // Notify both users about the like
                    await Clients.Users(new List<string> { username, otherUser.Username })
                        .SendAsync("PrivateMessageLiked", messageId, message.LikesCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LikePrivateMessage: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred liking the message");
            }
        }

        // Edit a private message
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

                // Only allow the sender to edit the message
                if (message.SenderId != user.Id)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only edit your own messages");
                    return;
                }

                // Update the message
                message.Content = newContent;
                message.IsEdited = true;
                await _dbContext.SaveChangesAsync();

                // Get the other user in the conversation
                var otherUserId = message.Conversation.User1Id == user.Id
                    ? message.Conversation.User2Id
                    : message.Conversation.User1Id;

                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                if (otherUser != null)
                {
                    // Notify both users about the edit
                    await Clients.Users(new List<string> { username, otherUser.Username })
                        .SendAsync("PrivateMessageEdited", messageId, newContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EditPrivateMessage: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred editing the message");
            }
        }

        // Delete a private message
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

                // Only allow the sender to delete the message
                if (message.SenderId != user.Id)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "You can only delete your own messages");
                    return;
                }

                // Get conversation and other user info before deleting
                var conversationId = message.ConversationId;
                var otherUserId = message.Conversation.User1Id == user.Id
                    ? message.Conversation.User2Id
                    : message.Conversation.User1Id;
                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                // Delete the message
                _dbContext.PrivateMessages.Remove(message);
                await _dbContext.SaveChangesAsync();

                // Notify both users about the deletion
                if (otherUser != null)
                {
                    await Clients.Users(new List<string> { username, otherUser.Username })
                        .SendAsync("PrivateMessageDeleted", messageId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeletePrivateMessage: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ErrorMessage", "An error occurred deleting the message");
            }
        }
    }
}