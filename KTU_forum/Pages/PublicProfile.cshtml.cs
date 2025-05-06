using KTU_forum.Data;
using KTU_forum.Models;
using KTU_forum.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Linq;

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

        public UserModel ProfileUser { get; set; }

        public IActionResult OnGet(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("No username specified for public profile.");
                return RedirectToPage("/Error");
            }

            // Retrieve the requested user from the database
            ProfileUser = _context.Users.FirstOrDefault(u => u.Username == username);

            if (ProfileUser == null)
            {
                _logger.LogWarning($"User with Username {username} not found.");
                return RedirectToPage("/Error");
            }

            return Page();
        }
    }
}