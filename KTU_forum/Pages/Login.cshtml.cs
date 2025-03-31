using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace KTU_forum.Pages
{
    public class LoginModel : PageModel
    {
        private readonly TempDb _context;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(TempDb context, ILogger<LoginModel> logger)
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

            // Check if user exists
            var user = _context.Users.FirstOrDefault(u => u.Username == Username);

            if (user != null && VerifyPassword(Password, user.password))
            {
                // Create session
                HttpContext.Session.SetString("Username", user.Username); // Store username in session
                //HttpContext.Session.SetString("UserRole", "User"); // Optionally store user role or other data

                // Create a cookie to store authentication information
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30), 
                    HttpOnly = true, // Makes the cookie inaccessible to JavaScript (for security)
                    Secure = true, // Only send cookie over HTTPS (for production)
                    SameSite = SameSiteMode.Strict // Prevents CSRF attacks
                };

                // Store user data in cookies
                Response.Cookies.Append("Username", user.Username, cookieOptions);


                // Log successful login attempt (you can also log the IP address or user agent for auditing)
                _logger.LogInformation($"User {Username} logged in successfully.");
                // Redirect after successful login
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

            // Log the logout action (you can log the session user information here as well)
            _logger.LogInformation("User logged out successfully.");

            return RedirectToPage("/Index");
        }

        public bool VerifyPassword(string enteredPassword, string storedHashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHashedPassword);
        }
    }

}
