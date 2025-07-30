namespace WorkflowTime.Features.ProjectManagement.Queries
{
    public class ProjectSearchQueryParameters
    {
        public required string SearchingPhrase { get; set; }
        public int ResponseLimit { get; set; }
    }
}
