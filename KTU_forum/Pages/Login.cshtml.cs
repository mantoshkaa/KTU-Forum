using System;
using System.Linq;
using KTU_forum.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using KTU_forum.Data;
using KTU_forum.Services;

namespace KTU_forum.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context; // Use ApplicationDbContext
        private readonly ILogger<LoginModel> _logger;

        private readonly OnlineUserService _onlineUserService;

        public LoginModel(ApplicationDbContext context, ILogger<LoginModel> logger, OnlineUserService onlineUserService)
        {
            _context = context;
            _logger = logger;
            _onlineUserService = onlineUserService;
        }

        [BindProperty]
        public string Username { get; set; }
        [BindProperty]
        public string Password { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }


        public void OnGet()
        {

            ModelState.Clear();  // Clear any previous ModelState errors
        }

        public IActionResult OnPost()
        {
            // Check if user exists in the real PostgreSQL database
            var user = _context.Users.FirstOrDefault(u => u.Username == Username);

            if (user == null)
            {
                // Username doesn't exist
                ModelState.AddModelError("Username", "No account found with this username.");
                _logger.LogWarning($"Login attempt with non-existent username: {Username}.");
                return Page();
            }

            // Check if the password is correct
            if (!VerifyPassword(Password, user.PasswordHash))
            {
                // Password incorrect
                ModelState.AddModelError("Password", "Incorrect password.");
                _logger.LogWarning($"Incorrect password for user {Username}.");
                return Page();
            }

            if (user != null && VerifyPassword(Password, user.PasswordHash))
            {
                // Create session
                HttpContext.Session.SetString("Username", user.Username);

                // Add user to online users list
                _onlineUserService.AddUser(user.Username, user.ProfilePicturePath, user.Role);

                // Create a cookie to store authentication information
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };
                /*
                // Check if the user is verified
                if (!user.IsVerified)
                {
                    ModelState.AddModelError(string.Empty, "Please verify your email before logging in.");
                    return Page();
                }
                */
                
                //palieku kaip komentara darbar nes kolkas no one is verified :D 

                // Store user data in cookies
                Response.Cookies.Append("Username", user.Username, cookieOptions);

                // Log successful login attempt
                _logger.LogInformation($"User {Username} logged in successfully.");

                ReturnUrl ??= "/";
                return LocalRedirect(ReturnUrl);

            }

            // Log failed login attempt
            _logger.LogWarning($"Failed login attempt for user {Username}.");

            // Invalid login
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            var username = HttpContext.Session.GetString("Username");

            // Clear session data
            HttpContext.Session.Clear();

            // Remove user from online users list if username exists
            if (!string.IsNullOrEmpty(username))
            {
                _onlineUserService.RemoveUser(username);
            }

            // Delete cookies
            Response.Cookies.Delete("Username");

            // Log the logout action
            _logger.LogInformation("User logged out successfully.");

            return RedirectToPage("/Login");
        }

        public bool VerifyPassword(string enteredPassword, string storedHashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHashedPassword);
        }
    }
}
