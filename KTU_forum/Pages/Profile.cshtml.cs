using KTU_forum.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Linq;
using KTU_forum.Data;

namespace KTU_forum.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(ApplicationDbContext context, ILogger<ProfileModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // To hold user data
        public UserModel CurrentUser { get; set; }

        // Profile picture upload property
        [BindProperty]
        public IFormFile NewProfilePicture { get; set; }

        // Password change property
        [BindProperty]
        public string NewPassword { get; set; }

        // Bio update property
        [BindProperty]
        public string NewBio { get; set; }

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
                    CurrentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                }

                // Update bio if provided
                if (NewBio != null)
                {
                    CurrentUser.Bio = NewBio;
                }

                // Update profile picture if provided
                if (NewProfilePicture != null)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-pictures", NewProfilePicture.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await NewProfilePicture.CopyToAsync(stream);
                    }

                    // Update the profile picture path in the database
                    CurrentUser.ProfilePicturePath = "/profile-pictures/" + NewProfilePicture.FileName;
                }

                // Save changes
                _context.Users.Update(CurrentUser);
                await _context.SaveChangesAsync();

                // Set confirmation message in TempData
                TempData["SuccessMessage"] = "Your profile has been updated successfully.";

                return RedirectToPage(); // Reload the page with updated user info
            }

            return Page(); // Return the page if there were errors
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            // Get logged-in username from session
            string loggedInUsername = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(loggedInUsername))
            {
                _logger.LogWarning("No user is logged in.");
                return RedirectToPage("/Login");
            }

            // Find the user in the database
            var userToDelete = _context.Users.FirstOrDefault(u => u.Username == loggedInUsername);

            if (userToDelete == null)
            {
                _logger.LogWarning($"User with Username {loggedInUsername} not found for deletion.");
                return RedirectToPage("/Error");
            }

            // Optional: Delete profile picture file if it exists
            if (!string.IsNullOrEmpty(userToDelete.ProfilePicturePath))
            {
                var picturePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", userToDelete.ProfilePicturePath.TrimStart('/'));
                if (System.IO.File.Exists(picturePath))
                {
                    System.IO.File.Delete(picturePath);
                }
            }

            // Remove user from the database
            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();

            // Clear session and redirect to homepage
            HttpContext.Session.Clear();
            return RedirectToPage("/Index");
        }
    }
}