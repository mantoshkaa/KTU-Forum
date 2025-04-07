using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KTU_forum.Data;
using KTU_forum.Models;
using System.Linq;

namespace KTU_forum.Pages
{
    public class MessHallModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public string Username { get; set; }
        public string ProfilePicturePath { get; set; }

        public MessHallModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            Username = HttpContext.Session.GetString("Username");

            if (!string.IsNullOrEmpty(Username))
            {
                var user = _context.Users.FirstOrDefault(u => u.Username == Username);

                if (user != null)
                {
                    ProfilePicturePath = user.ProfilePicturePath ?? "/pfps/default.png";
                }
                else
                {
                    ProfilePicturePath = "/pfps/default.png";
                }
            }
            else
            {
                ProfilePicturePath = "/pfps/default.png";
            }
        }
    }
}