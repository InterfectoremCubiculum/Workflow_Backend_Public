using CsvHelper.Configuration.Attributes;

namespace WorkflowTime.Features.Summary.Dtos
{
    public class UserWorkSummaryDayByDayDto
    {
        [Ignore]
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public string? Email { get; set; }
        public DateOnly Date { get; set; }
        public string? TeamName { get; set; }
        public string? ProjectName { get; set; }
        public required int WorkMinutes { get; set; }
        public required int BreakMinutes { get; set; }
    }
}
