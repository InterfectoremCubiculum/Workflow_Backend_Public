using Newtonsoft.Json;

namespace WorkflowTime.Features.WorkLog.Dtos
{
    public class WorkflowParameters
    {
        public string Intent { get; set; } = default!;
        public string? Type { get; set; }
        public TimeSpan? AddTime { get; set; }
        public TimeSpan? SubtractTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
