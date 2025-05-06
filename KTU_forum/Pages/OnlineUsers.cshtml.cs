using System.Collections.Generic;
using System.Threading.Tasks;
using KTU_forum.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KTU_forum.Pages
{
    public class OnlineUsersModel : PageModel
    {
        private readonly OnlineUserService _onlineUserService;

        public OnlineUsersModel(OnlineUserService onlineUserService)
        {
            _onlineUserService = onlineUserService;
        }

        public IActionResult OnGet()
        {
            // Update current user's activity if logged in
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                _onlineUserService.UpdateUserActivity(username);
            }

            // Return list of online users as JSON
            return new JsonResult(_onlineUserService.GetOnlineUsers());
        }
    }
}