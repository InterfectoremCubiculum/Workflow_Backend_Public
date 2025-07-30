using System.ComponentModel.DataAnnotations;
using WorkflowTime.Enums;
using WorkflowTime.Features.UserManagment.Models;

namespace WorkflowTime.Features.DayOffs.Models
{
    public class DayOffRequest
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateOnly StartDate { get; set; }
        [Required]
        public DateOnly EndDate { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DayOffRequestStatus RequestStatus { get; set; } = DayOffRequestStatus.Pending;
        public Guid UserId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public virtual UserModel? User { get; set; }
    }
}
