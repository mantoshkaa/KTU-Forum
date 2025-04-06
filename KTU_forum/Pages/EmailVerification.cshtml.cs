using KTU_forum.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace KTU_forum.Pages
{
    public class EmailVerificationModel : PageModel
    {
        private readonly TempDb _context;

        public EmailVerificationModel(TempDb context)
        {
            _context = context;
        }

        public string Message { get; set; }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Message = "Invalid token.";
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
            {
                Message = "Invalid token.";
                return Page();
            }

            // Mark the user as verified
            user.EmailVerified = true;
            user.EmailVerificationToken = null; // Clear the token after successful verification
            await _context.SaveChangesAsync();

            Message = "Your email has been successfully verified.";
            return RedirectToPage("/Login");
        }
    }

}
