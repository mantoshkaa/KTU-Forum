using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace KTU_forum.Pages
{
    public class KeepAliveModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.SetString("Ping", DateTime.UtcNow.ToString());
            return new JsonResult(new { success = true });
        }
    }

}
