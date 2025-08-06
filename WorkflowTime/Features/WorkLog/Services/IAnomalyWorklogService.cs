namespace WorkflowTime.Features.WorkLog.Services
{
    public interface IAnomalyWorklogService
    {
        public Task CheckDailyWorkLogAnomalies();
    }
}
