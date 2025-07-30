namespace WorkflowTime.Features.UserManagement.Dtos
{
    public class UserCacheDto
    {
        public Guid Id { get; set; }
        public required string GivenName { get; set; }
        public required string Surname { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}
