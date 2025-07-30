namespace WorkflowTime.Queries
{
    public class UserTimelineWorklogQueryParameters
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public List<Guid> UserIds { get; set; } = [];
    }
}
