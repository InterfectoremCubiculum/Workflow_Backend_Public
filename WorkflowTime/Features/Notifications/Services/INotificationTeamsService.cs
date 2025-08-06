using Microsoft.Graph.Models;
using WorkflowTime.Enums;

namespace WorkflowTime.Features.Notifications.Services
{
    /// <summary>
    /// Teams notification service interface.
    /// </summary>
    public interface INotificationTeamsService
    {
        public Task SendNotification(Guid userId, string messageToSend);
        public Task SendNotification(UserRole? role, string messageToSend);

    }
}
