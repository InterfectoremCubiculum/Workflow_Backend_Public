namespace WorkflowTime.Features.UserManagement.Dtos
{
    public class GetUsersByGuidDto
    {
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public string? Email { get; set; }
    }
}
