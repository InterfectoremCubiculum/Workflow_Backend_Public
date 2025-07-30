using Newtonsoft.Json;

namespace WorkflowTime.Features.Bot.Services.AI
{
    public class WorkflowActionResult
    {
        [JsonProperty("intent", Required = Required.Always)]
        public string Intent { get; set; } = default!;

        [JsonProperty("type", Required = Required.Default)]
        public string? Type { get; set; }

        [JsonProperty("addTime", Required = Required.Default)]
        public TimeSpan? AddTime { get; set; }

        [JsonProperty("subtractTime", Required = Required.Default)]
        public TimeSpan? SubtractTime { get; set; }

        [JsonProperty("startTime", Required = Required.Default)]
        public DateTime? StartTime { get; set; }

        [JsonProperty("endTime", Required = Required.Default)]
        public DateTime? EndTime { get; set; }
    }

}
