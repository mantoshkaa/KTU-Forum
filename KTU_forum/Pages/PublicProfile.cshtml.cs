using KTU_forum.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Linq;
using KTU_forum.Data;
using Microsoft.AspNetCore.Http;

namespace KTU_forum.Pages
{
    public class PublicProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PublicProfileModel> _logger;

        public PublicProfileModel(ApplicationDbContext context, ILogger<PublicProfileModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // To hold user data of the profile being viewed
        public UserModel ProfileUser { get; set; }

        // To determine if this is the current user's own profile
        public bool IsOwnProfile { get; set; }

        public IActionResult OnGet(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("No username provided for public profile view");
                return RedirectToPage("/Index");
            }

            // Retrieve the user by username from the database
            ProfileUser = _context.Users.FirstOrDefault(u => u.Username == username);

            if (ProfileUser == null)
            {
                _logger.LogWarning($"User with Username {username} not found.");
                return RedirectToPage("/Error", new { message = "User not found" });
            }

            // Check if this is the current user's own profile
            string loggedInUsername = HttpContext.Session.GetString("Username");
            IsOwnProfile = !string.IsNullOrEmpty(loggedInUsername) && loggedInUsername == username;

            // If it's the user's own profile, redirect to the private profile page
            if (IsOwnProfile)
            {
                return RedirectToPage("/Profile");
            }

            return Page();
        }
    }
}