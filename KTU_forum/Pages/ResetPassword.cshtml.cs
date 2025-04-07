using System;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using KTU_forum.Models;
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(ApplicationDbContext context, ILogger<ResetPasswordModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }

        public bool ResetSucceeded { get; set; } = false;
        public bool ResetFailed { get; set; } = false;

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if the token is provided
            if (string.IsNullOrEmpty(Token))
            {
                ResetFailed = true;
                _logger.LogWarning("Reset token was missing from the request.");
                return Page();
            }

            // Find the user by reset token
            var user = await _context.Users
                .Where(u => u.PasswordResetToken == Token && u.PasswordResetTokenExpiry > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                ResetFailed = true;
                _logger.LogWarning("Invalid or expired reset token: {Token}", Token);
                return Page();
            }

            // Valid token, display the reset form
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Token))
            {
                ResetFailed = true;
                _logger.LogWarning("Reset token was missing from the request.");
                return Page();
            }

            // Validate password confirmation
            if (NewPassword != ConfirmPassword)
            {
                ResetFailed = true;
                _logger.LogWarning("Password and confirmation do not match.");
                return Page();
            }

            // Find the user by reset token
            var user = await _context.Users
                .Where(u => u.PasswordResetToken == Token && u.PasswordResetTokenExpiry > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                ResetFailed = true;
                _logger.LogWarning("Invalid or expired reset token: {Token}", Token);
                return Page();
            }

            // Update the user's password (hash it before saving)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            user.PasswordResetToken = null; // Invalidate the reset token
            user.PasswordResetTokenExpiry = null; // Clear expiry date

            await _context.SaveChangesAsync();

            ResetSucceeded = true;
            _logger.LogInformation("User {Username} successfully reset their password.", user.Username);

            return Page();
        }
    }
}
