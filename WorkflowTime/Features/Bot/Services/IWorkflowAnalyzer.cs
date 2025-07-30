using WorkflowTime.Features.Bot.Services.AI;

namespace WorkflowTime.Features.Bot.Services
{
    public interface IWorkflowAnalyzer
    {
        Task<WorkflowActionResult> AnalyzeWorkflow(string inputText, CancellationToken cancellationToken);
    }
}
