using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Data;
using KTU_forum.Models;
using KTU_forum.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KTU_forum.Services
{
    public class RoleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProfileModel> _logger;

        public RoleService(ApplicationDbContext context, IConfiguration configuration, ILogger<ProfileModel> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // Check and update a user's roles based on their activities
        public async Task<(bool changed, string newRoleName, string newRoleColor)> UpdateUserRoles(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.PrimaryRole)  // Add this line
                .Include(u => u.Messages)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return (false, null, null);

            string oldRoleName = user.PrimaryRole?.Name;

            // Get all roles
            var roles = await _context.Roles.ToListAsync();
            var adminRole = roles.FirstOrDefault(r => r.Name == "Admin");
            var newcomerRole = roles.FirstOrDefault(r => r.Name == "Newcomer");
            var memberRole = roles.FirstOrDefault(r => r.Name == "Member");
            var regularRole = roles.FirstOrDefault(r => r.Name == "Regular");
            var seniorRole = roles.FirstOrDefault(r => r.Name == "Senior");
            var expertRole = roles.FirstOrDefault(r => r.Name == "Expert");

            // Check for admin role
            var adminEmails = _configuration.GetSection("AdminSettings:AdminEmails").Get<List<string>>();
            bool isAdmin = adminEmails != null && adminEmails.Contains(user.Email);

            if (isAdmin && !user.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                await AssignRole(user.Id, adminRole.Id);
                user.PrimaryRoleId = adminRole.Id;
            }

            // For new users with no role yet, assign newcomer
            if (!isAdmin && !user.UserRoles.Any())
            {
                await AssignRole(user.Id, newcomerRole.Id);
                user.PrimaryRoleId = newcomerRole.Id;
            }

            // Check for member role (has at least one message)
            // Check for member role (has at least one message)
            if (user.Messages.Any() && !user.UserRoles.Any(ur => ur.Role.Name == "Member"))
            {
                await AssignRole(user.Id, memberRole.Id);

                // Remove newcomer role if they have it
                if (user.UserRoles.Any(ur => ur.Role.Name == "Newcomer"))
                {
                    await RemoveRole(user.Id, newcomerRole.Id);
                }

                // Set as primary role if they don't have a higher priority role
                // FIXED COMPARISON: Lower DisplayPriority means higher priority
                if (!isAdmin && (!user.PrimaryRoleId.HasValue ||
                    (user.PrimaryRole != null && user.PrimaryRole.DisplayPriority > memberRole.DisplayPriority)))
                {
                    user.PrimaryRoleId = memberRole.Id;
                }
            }

            // Check for regular role (has at least 10 messages)
            if (user.Messages.Count >= 10 && !user.UserRoles.Any(ur => ur.Role.Name == "Regular"))
            {
                await AssignRole(user.Id, regularRole.Id);

                // Set as primary role if they don't have a higher priority role
                // FIXED COMPARISON: Lower DisplayPriority means higher priority
                if (!isAdmin && (!user.PrimaryRoleId.HasValue ||
                    (user.PrimaryRole != null && user.PrimaryRole.DisplayPriority > regularRole.DisplayPriority)))
                {
                    user.PrimaryRoleId = regularRole.Id;
                }
            }

            // Check CreatedAt against a strictly calculated 30 days ago
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            _logger.LogInformation($"User {user.Username} created at: {user.CreatedAt}");
            _logger.LogInformation($"30 days ago: {thirtyDaysAgo}");
            _logger.LogInformation($"1 month ago: {oneMonthAgo}");
            _logger.LogInformation($"Time from creation: {DateTime.UtcNow - user.CreatedAt}");

            // Use a stricter comparison
            bool eligibleForSenior = (DateTime.UtcNow - user.CreatedAt).TotalDays >= 30;

            if (eligibleForSenior && !user.UserRoles.Any(ur => ur.Role.Name == "Senior"))
            {
                _logger.LogInformation($"Assigning Senior role to user {user.Username}");
                await AssignRole(user.Id, seniorRole.Id);

                // Set as primary role if they don't have a higher priority role
                if (!isAdmin && (!user.PrimaryRoleId.HasValue ||
                (user.PrimaryRole != null && user.PrimaryRole.DisplayPriority > seniorRole.DisplayPriority)))
                {
                    user.PrimaryRoleId = seniorRole.Id;
                }
            }

            // Save changes to the user
            await _context.SaveChangesAsync();

            bool changed = oldRoleName != user.PrimaryRole?.Name;

            return (changed, user.PrimaryRole?.Name, user.PrimaryRole?.Color);
        }

        // Assign a role to a user
        public async Task AssignRole(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
            {
                _context.UserRoles.Add(new UserRoleModel
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
        }

        // Remove a role from a user
        public async Task RemoveRole(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();
            }
        }

        // Manually assign the Expert role
        public async Task AssignExpertRole(int userId)
        {
            var roles = await _context.Roles.ToListAsync();
            var expertRole = roles.FirstOrDefault(r => r.Name == "Expert");

            if (expertRole != null)
            {
                await AssignRole(userId, expertRole.Id);

                // Update primary role to Expert
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.PrimaryRoleId = expertRole.Id;
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Get a user's primary role information
        public async Task<(string Name, string Color)> GetUserPrimaryRole(int userId)
        {
            var user = await _context.Users
                .Include(u => u.PrimaryRole)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.PrimaryRole != null)
            {
                return (user.PrimaryRole.Name, user.PrimaryRole.Color);
            }

            // Fallback to existing Role string if PrimaryRole is not set
            if (!string.IsNullOrEmpty(user?.Role))
            {
                return (user.Role, "#6c757d");  // Default gray color
            }

            return ("Member", "#77DD77");  // Default
        }
    }
}