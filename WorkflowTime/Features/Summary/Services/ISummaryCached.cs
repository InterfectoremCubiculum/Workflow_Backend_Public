using WorkflowTime.Features.Summary.Dtos;

namespace WorkflowTime.Features.Summary.Services
{
    public interface ISummaryCached
    {
        public Task<byte[]> ExportCsv(Guid token);
        public Task<Guid> CacheUserWork(List<UserWorkSummaryDto> userWorkSummaries);

    }
}
