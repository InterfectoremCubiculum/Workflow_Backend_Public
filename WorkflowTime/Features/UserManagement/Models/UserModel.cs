using System.ComponentModel.DataAnnotations;
using WorkflowTime.Enums;
using WorkflowTime.Features.DayOffs.Models;
using WorkflowTime.Features.ProjectManagement.Models;
using WorkflowTime.Features.TeamManagement.Models;
using WorkflowTime.Features.WorkLog.Models;

namespace WorkflowTime.Features.UserManagment.Models
{
    public class UserModel
    {
        [Key]
        public Guid Id { get; set; }
        public required string GivenName { get; set; }
        public required string Surname { get; set; }
        public UserRole Role { get; set; } = UserRole.None;
        public string? Email { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int? TeamId { get; set; }
        public virtual Team? Team { get; set; }
        public int? ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public virtual ICollection<DayOffRequest>? DayOffRequests { get; set; }
        public virtual ICollection<TimeSegment>? TimeSegments { get; set; }
    }
}
