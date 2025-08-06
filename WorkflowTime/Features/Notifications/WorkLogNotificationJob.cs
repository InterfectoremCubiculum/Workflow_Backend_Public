using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Features.Notifications.Models;
using WorkflowTime.Features.Notifications.Services;

namespace WorkflowTime.Features.NotificationsTeams
{
    public class WorkLogNotificationJob
    {
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly INotificationTeamsService _notificationTeamsService;

        public WorkLogNotificationJob
        (
            WorkflowTimeDbContext dbContext,
            ILogger<WorkLogNotificationJob> logger,
            INotificationService notificationService,
            INotificationTeamsService notificationTeamsService
        )
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _notificationTeamsService = notificationTeamsService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">Start/End</param>
        public async Task CheckWorkLogs(string type, DateOnly? date)
        {
            string title = "⚠ This Request Your Attention !!!";
            string message;

            List<Guid> usersToNotify;
            switch (type) 
            {

                case "Start":
                    usersToNotify = await CheckDatabaseForMissingStart(date ?? DateOnly.FromDateTime(DateTime.UtcNow));
                    if (usersToNotify.Count == 0) { return; }
                    message = "\n You did not record your work time";
                    break;

                case "End":
                    usersToNotify = await CheckDatabaseForMissingEnd();
                    if (usersToNotify.Count == 0) { return; }
                    message = "\n You need to record your end of work";
                    break;

                default:
                    throw new ArgumentException("Invalid type. Use 'Start' or 'End'.");
            }

            string finalMessage = title + message;

            ConcurrentBag<Notification> notifications = new ConcurrentBag<Notification>();
            var notifyTasks = usersToNotify.Select(async userId =>
            {
                await _notificationTeamsService.SendNotification(userId, finalMessage);
                var note = new Notification(title, message, userId);
                notifications.Add(note);
            });

            await Task.WhenAll(notifyTasks);

            await _notificationService.CreateNotificationsBatch(notifications.ToList());

            var NotifyBySignalRTasks = notifications.Select(async notification =>
            {
                await _notificationService.SendNotification(notification.UserId, notification);
            });

            await Task.WhenAll(NotifyBySignalRTasks);

        }
        private async Task<List<Guid>> CheckDatabaseForMissingStart(DateOnly date)
        {
            var usersToNotify = await _dbContext.Users
                .Where(u => !u.IsDeleted)
                .Where(u => !_dbContext.DayOffRequests // If user has no day off request
                    .Any(d => d.UserId == u.Id
                        && d.RequestStatus == DayOffRequestStatus.Approved
                        && !d.IsDeleted
                        && d.StartDate <= date
                        && d.EndDate >= date))
                .Where(u => !_dbContext.TimeSegments // If user has no time segment for the given date
                    .Any(ts => ts.UserId == u.Id
                        && ts.StartTime.Date == date.ToDateTime(TimeOnly.MinValue).Date
                        && ts.TimeSegmentType == TimeSegmentType.Work
                        && !ts.IsDeleted))
                .Select(u => u.Id)
                .ToListAsync();

            return usersToNotify;
        }
        private async Task<List<Guid>> CheckDatabaseForMissingEnd()
        {
            var usersToNotify = await _dbContext.Users
                .Where(u => !u.IsDeleted)
                .Where(u => _dbContext.TimeSegments
                    .Any(ts => ts.UserId == u.Id
                        && ts.EndTime == null
                        && ts.TimeSegmentType == TimeSegmentType.Work
                        && !ts.IsDeleted))
                .Select(u => u.Id)
                .ToListAsync();

            return usersToNotify;
        }
    }
}
