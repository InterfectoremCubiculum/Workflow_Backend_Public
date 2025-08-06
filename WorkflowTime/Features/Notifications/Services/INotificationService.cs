using WorkflowTime.Enums;
using WorkflowTime.Features.Notifications.Models;

namespace WorkflowTime.Features.Notifications.Services
{
    /// <summary>
    /// SingalR
    /// </summary>
    public interface INotificationService
    {
        Task<List<SendedNotificationDto>> GetNotifications(Guid userId, bool read);
        Task MarkNotifications(List<int> notificationsIds);
        Task CreateNotificationsBatch(List<Notification> notifications);
        Task CreateNotificationsBatch(UserRole? userToInformRole, Notification notifications);
        Task SendNotification(Guid userId, SendedNotificationDto noteToSend);
        Task SendNotification(Guid userId, Notification noteToSend);
        Task NotifyWorkStateChange(Guid userId, string? state);
        Task SendNotification(UserRole userToInformRole, Notification noteToSend);

    }
}
