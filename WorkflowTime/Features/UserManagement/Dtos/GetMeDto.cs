using System.ComponentModel.DataAnnotations;

namespace WorkflowTime.Features.UserManagement.Dtos
{
    public class GetMeDto
    {
        public Guid UserId { get; set; }
        public required string GivenName { get; set; }
        public required string Surname { get; set; }
        public string? Email { get; set; }
        public required string Role { get; set; }
    }
}
