using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // Import for logging
using BCrypt.Net;
using KTU_forum.Data;
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Pages
{
    public class RegistrationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationModel> _logger; // Logger for registration

        [BindProperty]
        public UserModel NewUser { get; set; }

        public RegistrationModel(ApplicationDbContext context, ILogger<RegistrationModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // **API-Like Endpoint for JavaScript to Check Username Uniqueness**
        public JsonResult OnGetCheckUsername(string username)
        {
            bool exists = _context.Users.Any(u => u.Username == username);
            return new JsonResult(new { isTaken = exists });
        }

        public IActionResult OnPost()
        {
            // Log the registration attempt
            _logger.LogInformation($"User attempted to register with username: {NewUser.Username}");

            if (!NewUser.Email.EndsWith("@ktu.lt"))
            {
                _logger.LogWarning($"Invalid email domain: {NewUser.Email}. Only @ktu.lt emails are allowed.");

                ModelState.AddModelError("Email", "Only emails from @ktu.lt are allowed.");
                return Page();
            }

            // Check if username already exists
            if (_context.Users.Any(u => u.Username == NewUser.Username))
            {
                _logger.LogWarning($"Username '{NewUser.Username}' is already taken.");

                ModelState.AddModelError("NewUser.Username", "This username is already taken.");
                return Page();
            }
            
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewUser.PasswordHash); // Hash the password before storing it
                       
            NewUser.PasswordHash = hashedPassword; // Store the hashed password in the database

            string token = Guid.NewGuid().ToString(); // or use any secure generator
            NewUser.EmailVerificationToken = token;
            NewUser.IsVerified = false;


            // Save the user 
            _context.Users.Add(NewUser);
            _context.SaveChanges();

            // Log successful registration
            _logger.LogInformation($"User '{NewUser.Username}' successfully registered.");

            return RedirectToPage("/Login"); // Redirect after successful registration
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Retrieve all users from the DATABASE asynchronously
                var users = await _context.Users.ToListAsync();

                // Log all users (just for debugging or inspection purposes)
                foreach (var user in users)
                {
                    _logger.LogInformation($"Retrieved user - Username: {user.Username}, Email: {user.Email}");
                }
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the database retrieval
                _logger.LogError($"An error occurred while retrieving users: {ex.Message}");
            }
        }

    }
}
