using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Communications.GetPresencesByUserId;
using Microsoft.Graph.Models;
using System.Collections.Concurrent;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Features.AdminPanel.Services;
using WorkflowTime.Features.Notifications.Models;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.WorkLog.Services;
using ZiggyCreatures.Caching.Fusion;

namespace WorkflowTime.Features.Teams.Graph
{
    public class PresensceManager
    {
        private readonly GraphServiceClient _graphClient;
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly ILogger<PresensceManager> _logger;
        private readonly IFusionCache _cache;
        private readonly IMapper _mapper;
        private readonly ISettingsService _settingsService;
        private readonly IWorkLogService _workLogService;
        private readonly INotificationService _notificationService;
        private readonly INotificationTeamsService _notificationTeamsService;
        public PresensceManager
        (
            GraphServiceClient graphClient,
            WorkflowTimeDbContext dbContext,
            ILogger<PresensceManager> logger,
            IFusionCache cache,
            IMapper mapper,
            ISettingsService settingsService,
            IWorkLogService workLogService,
            INotificationService notificationService,
            INotificationTeamsService notificationTeamsService
        )
        {
            _graphClient = graphClient;
            _dbContext = dbContext;
            _logger = logger;
            _cache = cache;
            _mapper = mapper;
            _settingsService = settingsService;
            _workLogService = workLogService;
            _notificationService = notificationService;
            _notificationTeamsService = notificationTeamsService;
        }

        /// <summary>
        /// Maximum 650 users can be requested at once.
        /// </summary>
        /// <returns></returns>
        public async Task CheckUsersPresence()
        {
            var usersIds = await _dbContext.Users
                .Where(u => !u.IsDeleted)
                .Select(u => u.Id.ToString())
                .ToListAsync();

            var requestBody = new GetPresencesByUserIdPostRequestBody
            {
                Ids = usersIds
            };

            var result = await _graphClient.Communications.GetPresencesByUserId.PostAsGetPresencesByUserIdPostResponseAsync(requestBody);
            _logger.LogInformation("CheckUsersPresence() triggered at {Time}", DateTime.UtcNow);
            await CachePresence(result.Value);

        }

        private async Task CachePresence(List<Presence> presences)
        {
            int checkingPresenceInterval = _settingsService.GetSettingByKey<int>("check_presence_interval");
            int minutesToStartBreak = _settingsService.GetSettingByKey<int>("max_time_away");
            int minutesToNotify = _settingsService.GetSettingByKey<int>("time_away_when_user_get_notification");

            if (presences == null || !presences.Any())
            {
                _logger.LogWarning("No presences found to cache.");
                return;
            }

            var presenceDtos = _mapper.Map<List<UserPresenceDto>>(presences);

            var previousPresence = await GetCachedPresence();

            var usersToNotify = new List<string>();
            var usersToStartBreak = new List<string>();

            foreach (var presence in presenceDtos)
            {

                var previous = previousPresence.Find(p => p.UserId == presence.UserId);
                if (previous != null && previous.Status == presence.Status)
                {
                    presence.Minutes = previous.Minutes + checkingPresenceInterval;
                    presence.UserNotified = previous.UserNotified;
                }
                else
                {
                    presence.Minutes = checkingPresenceInterval;
                    presence.UserNotified = false;
                }

                if (presence.Status == AvailabilityPresenceStatus.Away)
                {
                    int minutes = presence.Minutes;
                    if (minutes >= minutesToStartBreak)
                    {
                        usersToStartBreak.Add(presence.UserId);
                    }
                    else if (minutes >= minutesToNotify && !presence.UserNotified)
                    {
                        presence.UserNotified = true;
                        usersToNotify.Add(presence.UserId);
                    }
                }
            }

            try
            {
                await StartUsersBreak(usersToStartBreak);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting automatic break for users.");
            }

            try
            {
                await NotifyUsersAboutTheirAbsence(usersToNotify);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while notifying users about their presence status.");
            }

            await _cache.SetAsync("UserPresences", presenceDtos, TimeSpan.FromMinutes(checkingPresenceInterval + 1));
        }

        private async Task<List<UserPresenceDto>> GetCachedPresence()
        {
            return await _cache.GetOrDefaultAsync<List<UserPresenceDto>>("UserPresences")
                ?? [];
        }

        private async Task StartUsersBreak(List<string> usersIds)
        {
            var startTime = DateTime.UtcNow;
            var userGuids = usersIds.Select(id => Guid.Parse(id)).ToList();

            var lastSegments = await _dbContext.TimeSegments
                .Where(ts => userGuids.Contains(ts.UserId) && !ts.IsDeleted)
                .GroupBy(ts => ts.UserId)
                .Select(g => g.OrderByDescending(ts => ts.StartTime).FirstOrDefault())
                .ToListAsync();

            var onlyInWorks = lastSegments
                .Where(ts => ts.TimeSegmentType == TimeSegmentType.Work && ts.EndTime == null)
                .Select(ts => ts.UserId)
                .ToList();
            if (onlyInWorks.Count == 0)
                return;

            await _workLogService.StartBreakForUsers(onlyInWorks, startTime);
            string finalMessage = "You have been automatically put on break due to inactivity.";
            string title = "Automatic Break Notification";

            await SendNotifications(onlyInWorks, finalMessage, title);

            foreach (var user in onlyInWorks)
            {
                await _notificationService.NotifyWorkStateChange(user, TimeSegmentType.Break.ToString());
            }

        }
        private async Task NotifyUsersAboutTheirAbsence(List<string> usersIds)
        {
            var userGuids = usersIds.Select(id => Guid.Parse(id)).ToList();

            var lastSegments = await _dbContext.TimeSegments
                .Where(ts => userGuids.Contains(ts.UserId) && !ts.IsDeleted)
                .GroupBy(ts => ts.UserId)
                .Select(g => g.OrderByDescending(ts => ts.StartTime).FirstOrDefault())
                .ToListAsync();

            var onlyInWorks = lastSegments
                .Where(ts => ts.TimeSegmentType == TimeSegmentType.Work && ts.EndTime == null)
                .Select(ts => ts.UserId)
                .ToList();
            if (onlyInWorks.Count == 0)
                return;

            string finalMessage = "You have been away for a while. Please check your status.";
            string title = "Presence Notification";

            await SendNotifications(onlyInWorks, finalMessage, title);

        }

        private async Task SendNotifications(List<Guid> to, string message, string title)
        {
            ConcurrentBag<Notification> notifications = new ConcurrentBag<Notification>();

            var notifyTasks = to.Select(async userId =>
            {
                await _notificationTeamsService.SendNotification(userId, message);
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

        public async Task<List<UserPresenceDto>> GetUsersActivity()
        {
            var presenceDtos = await GetCachedPresence();

            if (presenceDtos == null || !presenceDtos.Any())
            {
                _logger.LogWarning("No user activity data found in cache.");
                return new List<UserPresenceDto>();
            }

            _logger.LogInformation("Retrieved activity for {UserCount} users at {Time}", presenceDtos.Count, DateTime.UtcNow);

            return presenceDtos;
        }

        public async Task<UserPresenceDto?> GetUserActivity(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetUserActivity called with null or empty userId.");
                throw new ArgumentNullException(nameof(userId));
            }

            var presenceDtos = await GetCachedPresence();

            if (presenceDtos == null || !presenceDtos.Any())
            {
                _logger.LogWarning("No user activity data found in cache for userId {UserId}.", userId);
                return null;
            }

            var userPresence = presenceDtos.Find(p => p.UserId == userId);

            if (userPresence == null)
            {
                _logger.LogWarning("No activity data found for userId {UserId}.", userId);
                return null;
            }
           
            return userPresence;
        }
    }

}
