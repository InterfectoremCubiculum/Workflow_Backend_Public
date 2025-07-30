namespace WorkflowTime.Features.Summary.Queries
{
    public class TeamsWorkSummaryQueriesParameters
    {
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public required List<int> TeamIds { get; set; }
    }
}
