namespace WorkflowTime.Features.Summary.Queries
{
    public class ProjectsWorkSummaryQueriesParameters
    {
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public required List<int> ProjectIds { get; set; }
        public bool IsDayByDay { get; set; } = false;

    }
}
