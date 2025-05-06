using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using KTU_forum.Models;

namespace KTU_forum.Services
{
    public class OnlineUserService
    {
        private readonly ConcurrentDictionary<string, OnlineUserInfo> _onlineUsers = new ConcurrentDictionary<string, OnlineUserInfo>();
        private readonly ILogger<OnlineUserService> _logger;

        public OnlineUserService(ILogger<OnlineUserService> logger)
        {
            _logger = logger;
        }

        public void AddUser(string username, string profilePicture, string role)
        {
            var userInfo = new OnlineUserInfo
            {
                Username = username,
                ProfilePicturePath = string.IsNullOrEmpty(profilePicture) ? "/profile-pictures/default.png" : profilePicture,
                Role = role,
                LastActivity = DateTime.UtcNow
            };

            _onlineUsers.AddOrUpdate(username, userInfo, (key, oldValue) => userInfo);
            _logger.LogInformation($"User {username} is now online. Total online users: {_onlineUsers.Count}");
        }

        public void RemoveUser(string username)
        {
            if (!string.IsNullOrEmpty(username) && _onlineUsers.TryRemove(username, out _))
            {
                _logger.LogInformation($"User {username} is now offline. Total online users: {_onlineUsers.Count}");
            }
        }

        public void UpdateUserActivity(string username)
        {
            if (!string.IsNullOrEmpty(username) && _onlineUsers.TryGetValue(username, out var userInfo))
            {
                userInfo.LastActivity = DateTime.UtcNow;
                _onlineUsers[username] = userInfo;
            }
        }

        public List<OnlineUserInfo> GetOnlineUsers()
        {
            // Consider users inactive after 5 minutes without activity
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            // Remove inactive users first
            foreach (var user in _onlineUsers.Where(u => u.Value.LastActivity < cutoffTime).ToList())
            {
                _onlineUsers.TryRemove(user.Key, out _);
            }

            return _onlineUsers.Values.OrderBy(u => u.Username).ToList();
        }
    }

    public class OnlineUserInfo
    {
        public string Username { get; set; }
        public string ProfilePicturePath { get; set; }
        public string Role { get; set; }
        public DateTime LastActivity { get; set; }
    }
}