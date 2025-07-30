using System.Security.Claims;
using WorkflowTime.Exceptions;

namespace WorkflowTime.Features.UserManagement.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; private set; }
        public string? Email { get; private set; }
        public string? GivenName { get; private set; }
        public string? Surname { get; private set; }
        public IReadOnlyList<string> Roles { get; private set; } = [];

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity?.IsAuthenticated == true)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var oid = user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");

            if (!Guid.TryParse(oid, out var userId))
                throw new UnauthorizedAccessException("User 'oid' claim is missing or invalid.");

            UserId = userId;
            Email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("emails");
            GivenName = user.FindFirstValue(ClaimTypes.GivenName);
            Surname = user.FindFirstValue(ClaimTypes.Surname);
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList().AsReadOnly();

            if (!Roles.Any())
                throw new ForbiddenException("User has no assigned roles.");
        }
    }
}
