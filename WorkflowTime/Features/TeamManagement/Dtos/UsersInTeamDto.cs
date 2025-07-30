namespace WorkflowTime.Features.TeamManagement.Dtos
{
    public class UsersInTeamDto
    {
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public string? Email { get; set; }
    }
}
