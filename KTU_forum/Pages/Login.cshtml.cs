using System;
using System.Linq;
using KTU_forum.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using KTU_forum.Data;

namespace KTU_forum.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context; // Use ApplicationDbContext
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(ApplicationDbContext context, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string Username { get; set; }
        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
            ModelState.Clear();  // Clear any previous ModelState errors
        }

        public IActionResult OnPost()
        {
            // Check if user exists in the real PostgreSQL database
            var user = _context.Users.FirstOrDefault(u => u.Username == Username);

            if (user != null && VerifyPassword(Password, user.PasswordHash))
            {
                // Create session
                HttpContext.Session.SetString("Username", user.Username);

                // Create a cookie to store authentication information
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };

                // Store user data in cookies
                Response.Cookies.Append("Username", user.Username, cookieOptions);

                // Log successful login attempt
                _logger.LogInformation($"User {Username} logged in successfully.");
                return RedirectToPage("/Rooms");
            }

            // Log failed login attempt
            _logger.LogWarning($"Failed login attempt for user {Username}.");

            // Invalid login
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            // Clear session data
            HttpContext.Session.Clear();

            // Delete cookies
            Response.Cookies.Delete("Username");

            // Log the logout action
            _logger.LogInformation("User logged out successfully.");

            return RedirectToPage("/Index");
        }

        public bool VerifyPassword(string enteredPassword, string storedHashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHashedPassword);
        }
    }
}
