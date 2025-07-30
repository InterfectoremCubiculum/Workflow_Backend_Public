namespace WorkflowTime.Features.Notifications
{
    public class SendedNotificationDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
