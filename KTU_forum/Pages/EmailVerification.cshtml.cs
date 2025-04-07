using System;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using KTU_forum.Models;

namespace KTU_forum.Pages
{
    public class EmailVerificationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailVerificationModel> _logger;

        public EmailVerificationModel(ApplicationDbContext context, ILogger<EmailVerificationModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        public bool VerificationSucceeded { get; set; } = false;
        public bool VerificationFailed { get; set; } = false;

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Token))
            {
                VerificationFailed = true;
                _logger.LogWarning("Verification token was missing from the request.");
                return Page();
            }

            var user = await _context.Users
                .Where(u => u.EmailVerificationToken == Token && !u.IsVerified)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                VerificationFailed = true;
                _logger.LogWarning("Invalid or expired verification token: {Token}", Token);
                return Page();
            }

            user.IsVerified = true;
            user.EmailVerificationToken = null; // Invalidate the token
            await _context.SaveChangesAsync();

            VerificationSucceeded = true;
            _logger.LogInformation("User {Username} successfully verified their email.", user.Username);

            return Page();
        }
    }
}
