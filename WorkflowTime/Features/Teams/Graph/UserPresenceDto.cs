using WorkflowTime.Enums;

namespace WorkflowTime.Features.Teams.Graph
{
    public class UserPresenceDto
    {
        public required string UserId { get; set; }
        public int Minutes { get; set; } = 0;
        public AvailabilityPresenceStatus Status { get; set; } = AvailabilityPresenceStatus.Offline;
        public bool UserNotified { get; set; } = false;
    }
}
