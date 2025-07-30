using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WorkflowTime.Features.Hubs
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
        }
    }
}
