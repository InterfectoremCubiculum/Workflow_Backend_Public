using Microsoft.EntityFrameworkCore;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Features.AdminPanel.Services;
using WorkflowTime.Features.Notifications.Models;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.UserManagment.Models;
using WorkflowTime.Features.WorkLog.Models;
using ZiggyCreatures.Caching.Fusion;

namespace WorkflowTime.Features.WorkLog.Services
{
    public class AnomalyWorklogService : IAnomalyWorklogService
    {
        private readonly ILogger<AnomalyWorklogService> _logger;
        private readonly ISettingsService _settingsService;
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly INotificationTeamsService _notificationTeamsService;
        private readonly IFusionCache _fusionCache;
        private readonly IWorkLogService _workLogService;
        public AnomalyWorklogService
        (
            ILogger<AnomalyWorklogService> logger,
            ISettingsService settingsService,
            WorkflowTimeDbContext dbContext,
            INotificationService notificationService,
            INotificationTeamsService notificationTeamsService,
            IFusionCache fusionCache,
            IWorkLogService workLogService
        )
        {
            _logger = logger;
            _settingsService = settingsService;
            _dbContext = dbContext;
            _notificationTeamsService = notificationTeamsService;
            _notificationService = notificationService;
            _fusionCache = fusionCache;
            _workLogService = workLogService;
        }

        public async Task CheckDailyWorkLogAnomalies()
        {
            var today = DateTime.Today;
            var nowTime = DateTime.UtcNow;

            //Skip between 23:00 and 03:00
            if (nowTime.Hour >= 23 || nowTime.Hour < 1)
            {
                return;
            }

            var maxWorkTime = _settingsService.GetSettingByKey<int>("max_work_time"); // In hours
            var maxBreakTime = _settingsService.GetSettingByKey<int>("max_time_break"); // In minutes
            var maxSummariseBreakTime = _settingsService.GetSettingByKey<int>("max_summarise_break_time"); // In minutes
            var workLogNotificationStart = _settingsService.GetSettingByKey<DateTime>("work_log_notification_start"); // Start time for work log notifications when someone not started work
            var workLogNotificationEnd = _settingsService.GetSettingByKey<DateTime>("work_log_notification_end"); // End time for work log notifications when someone not ended work

            var usersTodaysTimeSegments = await _dbContext.TimeSegments
                .Where(ts => ts.StartTime >= today && !ts.IsDeleted)
                .ToListAsync();
            
            DateOnly todayDateOnly = DateOnly.FromDateTime(today);

            var dayOffs = await _dbContext.DayOffRequests
                .Where(dor => dor.RequestStatus == DayOffRequestStatus.Approved
                           && dor.StartDate <= todayDateOnly
                           && dor.EndDate >= todayDateOnly)
                .ToListAsync();

            var userIdsWithDayOffToday = dayOffs.Select(d => d.UserId).Distinct().ToHashSet();
            var groupedByUser = usersTodaysTimeSegments
                .Where(ts => !userIdsWithDayOffToday.Contains(ts.UserId))
                .GroupBy(ts => ts.UserId);

            var users = await _dbContext.Users.ToDictionaryAsync(u => u.Id);

            foreach (var userSegments in groupedByUser)
            {
                var userId = userSegments.Key;
                var workSegments = userSegments.Where(ts => ts.TimeSegmentType == TimeSegmentType.Work);
                var breakSegments = userSegments.Where(ts => ts.TimeSegmentType == TimeSegmentType.Break);

                users.TryGetValue(userId, out var user);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found in the database.");
                    continue;
                }

                var totalBreakMinutes = breakSegments.Sum(bs =>
                    bs.DurationInSeconds.HasValue
                        ? bs.DurationInSeconds.Value / 60
                        : (bs.EndTime.HasValue ? (bs.EndTime.Value - bs.StartTime).TotalMinutes : (nowTime - bs.StartTime).TotalMinutes));

                if (totalBreakMinutes > maxSummariseBreakTime)
                {
                    await LogSpendendTooManyTimesOnBreaks(user, totalBreakMinutes);
                }

                // Check for late working sessions 
                var firstWorkSegment = workSegments.OrderBy(ws => ws.StartTime).FirstOrDefault();
                
                if (firstWorkSegment != null && firstWorkSegment.StartTime > workLogNotificationStart)
                { 
                    await LogLateLoginAsync(user, firstWorkSegment.StartTime);
                }

                // Check for long work sessions
                var totalWorkSeconds = workSegments
                    .Where(ws => ws.DurationInSeconds.HasValue)
                    .Sum(ws => ws.DurationInSeconds.Value);

                var totalWorkHours = totalWorkSeconds / 3600; // hours

                if (totalWorkHours > maxWorkTime)
                {
                    await LogLongWorkSession(user, workSegments.OrderBy(o => o.StartTime).ToList(), (int)totalWorkHours);
                }

                // Check for no breaks
                bool isLongWorkWithoutBreaks = totalBreakMinutes == 0 && totalWorkHours > (maxWorkTime / 2.0) + 2;
                if (isLongWorkWithoutBreaks)
                {
                    await LogNoBreaks(user, (int)totalWorkHours);
                }

                // Check for too long breaks
                var tooLongBreak = breakSegments.Where(
                    bs => bs.EndTime == null
                    && ((nowTime - bs.StartTime).TotalMinutes > maxBreakTime))
                .FirstOrDefault();


                if (tooLongBreak != null)
                {
                    var breakMinutes = (tooLongBreak.StartTime - nowTime).TotalMinutes;
                    await LogTooLongBreaks(user, -breakMinutes);
                }
            }

            _logger.LogInformation("Searching for anomalies in the work log.");
        }
        public async Task LogTooLongBreaks(UserModel user, double minutes)
        {
            string message = $"User {user.GivenName} {user.Surname} ({user.Id}) had a break for over {(int)minutes} minutes !!!.";
            await NotifyAdmins(message);
            await MarkAsNotified(user.Id, AnomalyType.TooLongBreak);

            await _workLogService.EndWork(user.Id, null);
            await _notificationService.NotifyWorkStateChange(user.Id, null);

            string messageForUser = $"You had a break for over {(int)minutes} minutes. Please try to take breaks that are not too long. Your Break will be endend";
            await NotifyUser(user, messageForUser);
        }

        public async Task LogSpendendTooManyTimesOnBreaks(UserModel user, double minutes)
        {
            if (await IsAlreadyNotified(user.Id, AnomalyType.SpentTooMuchTimeOnBreak))
                return;

            string message = $"User {user.GivenName} {user.Surname} ({user.Id}) had spend in break over {(int)minutes} minutes !!!.";
            await NotifyAdmins(message);
            await MarkAsNotified(user.Id, AnomalyType.SpentTooMuchTimeOnBreak);

            string messageForUser = $"You had a breaks for over {(int)minutes} minutes";
            await NotifyUser(user, messageForUser);
        }

        public async Task LogLateLoginAsync(UserModel user, DateTime loginTime)
        {
            if (await IsAlreadyNotified(user.Id, AnomalyType.LateLogin))
                return;

            string message = $"User {user.GivenName} {user.Surname} ({user.Id}) logged in late at {loginTime}.";
            await NotifyAdmins(message);
            await MarkAsNotified(user.Id, AnomalyType.LateLogin);

            string messageForUser = $"You logged in late at {loginTime}. Please try to start your work on time.";
            await NotifyUser(user, messageForUser);
        }

        public async Task LogLongWorkSession(UserModel user, List<TimeSegment> listOfTimeSegments, int hours)
        {
            if (await IsAlreadyNotified(user.Id, AnomalyType.LongWorkSession))
                return;

            var last = listOfTimeSegments.Last();
            last.EndTime ??= DateTime.UtcNow;

            await _workLogService.EndWork(user.Id, null);
            await _notificationService.NotifyWorkStateChange(user.Id, null);

            string dates = string.Join("\n", listOfTimeSegments.Select(segment =>
                $"{segment.StartTime.ToShortDateString()} {segment.StartTime.ToShortTimeString()} - {segment.EndTime?.ToShortTimeString() ?? "Ongoing"}"));

            string message = $"User {user.GivenName} {user.Surname} ({user.Id}) had a long work session\n: {dates} with over {hours} hours of work.";
            await NotifyAdmins(message);
            await MarkAsNotified(user.Id, AnomalyType.LongWorkSession);

            string messageForUser = $"You had a long work session with over {hours} hours of work. Please take care of your health. I am ending your work now";
            await NotifyUser(user, messageForUser);
        }

        public async Task LogNoBreaks(UserModel user, int hours)
        {
            if (await IsAlreadyNotified(user.Id, AnomalyType.NoBreaks))
                return;

            string message = $"User {user.GivenName} {user.Surname} ({user.Id}) worked for {hours} hours without breaks.";
            await NotifyAdmins(message);
            await MarkAsNotified(user.Id, AnomalyType.NoBreaks);
            await NotifyUser(user, message);
        }

        private async Task NotifyAdmins(string message)
        {
            await _notificationTeamsService.SendNotification(UserRole.Admin, message);

            Notification note = new Notification("Too long break", message);
            await _notificationService.CreateNotificationsBatch(UserRole.Admin, note);
            await _notificationService.SendNotification(UserRole.Admin, note);
        }

        private async Task NotifyUser(UserModel user, string message)
        {
            Notification note = new Notification($"Anomaly Detected", $"{message} Admin might take some actions", user.Id);
            _dbContext.Add(note);
            await _dbContext.SaveChangesAsync();
            await _notificationService.SendNotification(user.Id, note);
            await _notificationTeamsService.SendNotification(user.Id, message);
        }

        private string GetCacheKey(Guid userId, AnomalyType anomalyType)
        {
            return $"anomaly:{userId}:{anomalyType}";
        }

        private async Task<bool> IsAlreadyNotified(Guid userId, AnomalyType anomalyType)
        {
            string key = GetCacheKey(userId, anomalyType);

            var cacheValue = await _fusionCache.TryGetAsync<bool>(key);

            if (cacheValue.HasValue)
                return cacheValue.Value;

            return false;
        }

        private async Task MarkAsNotified(Guid userId, AnomalyType anomalyType)
        {
            string key = GetCacheKey(userId, anomalyType);

            var now = DateTime.Now;
            var expirationTime = DateTime.Today.AddDays(1).AddMinutes(5); // 00:05
            var ttl = expirationTime - now;

            await _fusionCache.SetAsync(key, true, ttl);
        }
    }
}
