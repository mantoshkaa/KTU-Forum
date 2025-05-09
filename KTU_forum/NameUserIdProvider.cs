using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace KTU_forum
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // Use the username from the HttpContext Session as the user identifier
            // This must be set when the user logs in
            return connection.GetHttpContext().Session.GetString("Username");
        }
    }
}