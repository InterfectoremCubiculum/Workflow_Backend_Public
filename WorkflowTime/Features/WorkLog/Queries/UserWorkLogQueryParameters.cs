namespace WorkflowTime.Features.WorkLog.Queries
{
    public class UserWorkLogQueryParameters
    {
        public Guid? UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
