using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KTU_forum.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }



        public IActionResult OnPost()
        {
            if (!Email.EndsWith("@ktu.lt"))
            {
                ModelState.AddModelError("Email", "Only emails from @ktu.lt are allowed.");
                return Page();
            }

            // Continue with the authentication process (e.g., check password, etc.)

            return RedirectToPage("/Rooms"); // Redirect after successful login
        }
    }

}
