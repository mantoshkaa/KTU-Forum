using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using KTU_forum.Data;      // <-- your ApplicationDbContext namespace
using Microsoft.EntityFrameworkCore;

namespace KTU_forum.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;

        public ChatHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SendMessage(string user, string message)
        {
            // Get the user from DB
            var dbUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == user);

            // If user not found or profile picture is null, use default
            string profilePic = dbUser?.ProfilePicturePath ?? "/pfps/default.png";

            // Send all three values: username, message, and profile picture path
            await Clients.All.SendAsync("ReceiveMessage", user, message, profilePic);
        }
    }
}

