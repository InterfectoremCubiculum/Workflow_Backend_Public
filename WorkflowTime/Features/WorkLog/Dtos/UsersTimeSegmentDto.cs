using WorkflowTime.Enums;

namespace WorkflowTime.Features.WorkLog.Dtos
{
    public class UsersTimeSegmentDto
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationInSeconds { get; set; }
        public TimeSegmentType TimeSegmentType { get; set; }
    }
}
