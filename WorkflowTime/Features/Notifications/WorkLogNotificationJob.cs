using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using System.Collections.Concurrent;
using WorkflowTime.Configuration;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Features.Notifications.Models;
using WorkflowTime.Features.Notifications.Services;

namespace WorkflowTime.Features.NotificationsTeams
{
    public class WorkLogNotificationJob
    {
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly CloudAdapter _adapter;
        private readonly ILogger<WorkLogNotificationJob> _logger;
        private readonly INotificationService _notificationService;
        private readonly ResiliencePipeline _notifyTeamsPipeLine;

        private readonly string _appId;
        private readonly string _serviceUrl;
        private readonly string _tenantId;


        public WorkLogNotificationJob
        (
            WorkflowTimeDbContext dbContext,
            CloudAdapter adapter,
            ILogger<WorkLogNotificationJob> logger,
            INotificationService notificationService,
            IOptions<AzureAdOptions> azureAdOptions,
            IOptions<MicrosoftAppOptions> microsoftAppOptions,
            ResiliencePipelineProvider<string> pipelineProvider

        )
        {
            _dbContext = dbContext;
            _adapter = adapter;
            _notificationService = notificationService;
            _logger = logger;
            _notifyTeamsPipeLine = pipelineProvider.GetPipeline("TeamsNotificationPipeLine");

            _appId = microsoftAppOptions.Value.AppId;
            _serviceUrl = microsoftAppOptions.Value.ServiceUrl;
            _tenantId = azureAdOptions.Value.TenantId;
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
                await NotifyOnTeams(userId, finalMessage);
                var note = new Notification(title, message, userId);
                notifications.Add(note);
            });

            await Task.WhenAll(notifyTasks);

            await _notificationService.CreateNotificationsBatch(notifications.ToList());

            var NotifyBySignalRTasks = notifications.Select(async notification =>
            {
                await _notificationService.NotifySomething(notification.UserId, notification);
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

        private async Task NotifyOnTeams(Guid userId, string finalMessage)
        {
            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Bot = new ChannelAccount(_appId),
                Members = new List<ChannelAccount>
                {
                        new ChannelAccount(id:$"{userId.ToString()}")
                },
                TenantId = _tenantId,
                ChannelData = new { tenant = new { id = _tenantId } }
            };

            try
            {
                await _notifyTeamsPipeLine.ExecuteAsync(async ct =>
                {
                    await _adapter.CreateConversationAsync(
                        _appId,
                        "msteams",
                        _serviceUrl,
                        null,
                        conversationParameters,
                        async (turnContext, innerCt) =>
                        {
                            await turnContext.SendActivityAsync(finalMessage, cancellationToken: innerCt);
                        },
                        ct);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to user {userId}");
            }
        }
    }
}
