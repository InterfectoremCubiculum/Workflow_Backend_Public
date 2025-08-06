namespace WorkflowTime.Features.Teams.Bot.Services.AI
{
    public interface IWorkflowAnalyzer
    {
        Task<WorkflowActionResult> AnalyzeWorkflow(string inputText, CancellationToken cancellationToken);
    }
}
