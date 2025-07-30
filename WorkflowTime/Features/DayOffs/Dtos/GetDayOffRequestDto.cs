using Azure.Identity;
using WorkflowTime.Enums;
using WorkflowTime.Features.DayOffs.Models;

namespace WorkflowTime.Features.DayOffs.Dtos
{
    public class GetDayOffRequestDto
    {
        public int Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DayOffRequestStatus RequestStatus { get; set; }
        public DateTime RequestDate { get; set; }
        public string? UserName { get; set; }
        public string? UserSurname { get; set; }
    }
}
