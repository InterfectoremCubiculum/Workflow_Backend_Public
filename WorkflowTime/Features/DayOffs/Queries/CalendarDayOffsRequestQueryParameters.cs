using WorkflowTime.Enums;

namespace WorkflowTime.Features.DayOffs.Queries
{
    public class CalendarDayOffsRequestQueryParameters
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }

        public List<DayOffRequestStatus>? DayOffRequestStatuses { get; set; }
        public Guid UserId { get; set; }
    }
}
