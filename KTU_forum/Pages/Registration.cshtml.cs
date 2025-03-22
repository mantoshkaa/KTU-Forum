using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTU_forum.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KTU_forum.Pages
{
    public class RegistrationModel : PageModel
    {
        private readonly Models.TempDb _context;


        [BindProperty]
        public User NewUser { get; set; }


        /**
        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }
        **/


        public RegistrationModel(Models.TempDb context)
        {
            _context = context;
        }


        // **API-Like Endpoint for JavaScript to Check Username Uniqueness**
        public JsonResult OnGetCheckUsername(string username)
        {
            bool exists = _context.Users.Any(u => u.Username == username);
            return new JsonResult(new { isTaken = exists });
        }

        public IActionResult OnPost()
        {
            if(!NewUser.email.EndsWith("@ktu.lt"))
            {
                ModelState.AddModelError("Email", "Only emails from @ktu.lt are allowed.");
                return Page();
            }

            // Check if username already exists
            if (_context.Users.Any(u => u.Username == NewUser.Username))
            {
                ModelState.AddModelError("NewUser.Username", "This username is already taken.");
                return Page();
            }

            // Save the user (Placeholder since you don't have a real database yet)
            _context.Users.Add(NewUser);
            _context.SaveChanges();

            // Continue with the authentication process (e.g., check password, etc.)

            return RedirectToPage(); // Redirect after successful login
        }

        public void OnGet()
        {
            // Retrieve all users from the in-memory database
            var users = _context.Users.ToList();

            // Output the users to the console or inspect them in the debug output
            foreach (var user in users)
            {
                Console.WriteLine($"Username: {user.Username}, Email: {user.email}");
            }
        }


    }

}
