namespace WorkflowTime.Features.UserManagement.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string? Email { get; }
        string? GivenName { get; }
        string? Surname { get; }
        IReadOnlyList<string> Roles { get; }
    }
}
