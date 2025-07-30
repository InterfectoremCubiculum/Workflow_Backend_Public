using WorkflowTime.Enums;

namespace WorkflowTime.Features.WorkLog.Dtos
{
    public class WidgetSyncDto
    {
        public TimeSegmentType TimeSegmentType { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationInSeconds { get; set; }
    }
}
