namespace WorkflowTime.Features.WorkLog.Dtos
{
    public class ProjectInTimeLineDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<UsersTimelineWorklogDto> TimeLines { get; set; } = [];
    }
}
