using WorkflowTime.Enums;

namespace WorkflowTime.Features.DayOffs.Dtos
{
    public class GetCreatedDayOffRequestDto
    {
        public int Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DayOffRequestStatus RequestStatus { get; set; }
        public DateTime RequestDate { get; set; }
    }
}
