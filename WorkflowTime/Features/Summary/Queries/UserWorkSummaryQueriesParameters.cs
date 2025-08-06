namespace WorkflowTime.Features.Summary.Queries
{
    public class UserWorkSummaryQueriesParameters
    {
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public required List<Guid> UserIds { get; set; }
        public bool IsDayByDay { get; set; } = false;
    }
}
