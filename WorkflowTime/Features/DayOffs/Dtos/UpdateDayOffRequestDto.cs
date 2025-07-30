using WorkflowTime.Enums;

namespace WorkflowTime.Features.DayOffs.Dtos
{
    public class UpdateDayOffRequestDto
    {
        public int Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DayOffRequestStatus RequestStatus { get; set; }
    }
}
