using CsvHelper.Configuration.Attributes;

namespace WorkflowTime.Features.Summary.Dtos
{
    public class UserWorkSummaryDto
    {
        [Ignore]
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public string? TeamName { get; set; }
        public string? ProjectName { get; set; }
        public required string Surname { get; set; }
        public string? Email { get; set; }
        public TimeSpan TotalWorkHours { get; set; }
        public TimeSpan TotalBreakHours { get; set; }
        public int TotalDaysWorked { get; set; }
        public int TotalDaysOff { get; set; }
    }
}
