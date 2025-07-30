using WorkflowTime.Enums;

namespace WorkflowTime.Features.WorkLog.Dtos
{
    public class UsersTimelineWorklogDto
    {
        public int Id { get; set; }
        public TimeSegmentType TimeSegmentType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationInSeconds { get; set; }
        public Guid UserId { get; set; }
    }
}
