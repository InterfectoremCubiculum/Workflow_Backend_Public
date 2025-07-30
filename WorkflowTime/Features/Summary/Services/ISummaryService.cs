using WorkflowTime.Features.Summary.Dtos;
using WorkflowTime.Features.Summary.Queries;

namespace WorkflowTime.Features.Summary.Services
{
    public interface ISummaryService
    {
        public Task<List<UserWorkSummaryDto>> GetWorkSummariesForUsers(UserWorkSummaryQueriesParameters parameters);
        public Task<List<UserWorkSummaryDto>> GetTeamsWorkSummary(TeamsWorkSummaryQueriesParameters parameters);
        public Task<List<UserWorkSummaryDto>> GetProjectsWorkSummary(ProjectsWorkSummaryQueriesParameters parameters);
    }
}
