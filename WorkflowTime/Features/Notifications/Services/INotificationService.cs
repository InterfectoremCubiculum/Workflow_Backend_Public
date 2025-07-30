using WorkflowTime.Features.Notifications.Models;

namespace WorkflowTime.Features.Notifications.Services
{
    public interface INotificationService
    {
        Task<List<SendedNotificationDto>> GetNotifications(Guid userId, bool read);
        Task MarkNotifications(List<int> notificationsIds);
        Task CreateNotificationsBatch(List<Notification> notifications);
        Task NotifySomething(Guid userId, SendedNotificationDto noteToSend);
        Task NotifySomething(Guid userId, Notification noteToSend);
        Task NotifyWorkStateChange(Guid userId, string? state);
    }
}
