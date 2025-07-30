using System.Text.Json.Serialization;
using WorkflowTime.Enums;

namespace WorkflowTime.Features.DayOffs.Dtos
{
    public class CreateDayOffRequestDto
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DayOffRequestStatus RequestStatus { get; set; } = DayOffRequestStatus.Pending;
        public CreateDayOffRequestDto()
        {}
    }
}
