namespace WorkflowTime.Features.TeamManagement.Queries
{
    public class TeamSearchQueryParameters
    {
        public required string SearchingPhrase { get; set; }
        public int ResponseLimit { get; set; } = 10;
    }
}
