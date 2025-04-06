using KTU_forum.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Linq;

namespace KTU_forum.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly TempDb _context;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(TempDb context, ILogger<ProfileModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // To hold user data
        public User CurrentUser { get; set; }

        // Profile picture upload property
        [BindProperty]
        public IFormFile NewProfilePicture { get; set; }

        // Password change property
        [BindProperty]
        public string NewPassword { get; set; }

        public void OnGet()
        {
            // Retrieve the username from the session
            string loggedInUsername = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(loggedInUsername))
            {
                _logger.LogWarning("No user is logged in.");
                RedirectToPage("/Login"); // Redirect to login page if not logged in
                return;
            }

            // Retrieve the current user from the database
            CurrentUser = _context.Users.FirstOrDefault(u => u.Username == loggedInUsername);

            if (CurrentUser == null)
            {
                _logger.LogWarning($"User with Username {loggedInUsername} not found.");
                RedirectToPage("/Error"); // Redirect to error page if user is not found
                return; // Ensure the method exits if no user is found
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Ensure CurrentUser is set before any operation
            string loggedInUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(loggedInUsername))
            {
                _logger.LogWarning("No user is logged in.");
                return RedirectToPage("/Login"); // Redirect to login page if no username in session
            }

            // Retrieve the current user from the database
            CurrentUser = _context.Users.FirstOrDefault(u => u.Username == loggedInUsername);

            if (CurrentUser == null)
            {
                _logger.LogWarning($"User with Username {loggedInUsername} not found.");
                return RedirectToPage("/Error"); // Redirect to error page if user is not found
            }

            if (ModelState.IsValid)
            {
                // Update password if provided
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    CurrentUser.password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                    _context.Users.Update(CurrentUser);
                    await _context.SaveChangesAsync();
                }

                // Update profile picture if provided
                if (NewProfilePicture != null)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", NewProfilePicture.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await NewProfilePicture.CopyToAsync(stream);
                    }

                    // Update the profile picture path in the database
                    _context.Users.Update(CurrentUser);
                    await _context.SaveChangesAsync();
                }

                return RedirectToPage(); // Reload the page with updated user info
            }

            return Page(); // Return the page if there were errors
        }

    }


}
