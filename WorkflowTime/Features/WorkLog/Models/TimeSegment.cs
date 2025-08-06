using System.ComponentModel.DataAnnotations;
using WorkflowTime.Enums;
using WorkflowTime.Features.UserManagment.Models;

namespace WorkflowTime.Features.WorkLog.Models
{
    public class TimeSegment
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public TimeSegmentType TimeSegmentType { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public int? DurationInSeconds { get; private set; }
        public bool RequestAction { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        [Required]
        public Guid UserId { get; set; }
        public virtual UserModel User { get; set; }
    }
}
