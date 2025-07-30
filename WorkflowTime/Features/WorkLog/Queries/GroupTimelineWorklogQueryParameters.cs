using WorkflowTime.Enums;

namespace WorkflowTime.Features.WorkLog.Queries
{
    public class GroupTimelineWorklogQueryParameters
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public int GroupId { get; set; }
    }
}
