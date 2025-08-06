using Hangfire;
using WorkflowTime.Configuration;
using WorkflowTime.Features.AdminPanel.Services;
using WorkflowTime.Features.DayOffs.Services;
using WorkflowTime.Features.NotificationsTeams;
using WorkflowTime.Features.Teams.Bot.Services;
using WorkflowTime.Features.Teams.Graph;
using WorkflowTime.Features.UserManagement.Services;
using WorkflowTime.Features.WorkLog.Services;

namespace WorkflowTime.Extensions
{
    public static class HangfireExtensions
    {
        public static void ConfigureRecurringJobs(IServiceProvider services, TeamsOptions teams, ISettingsService settingsService)
        {            
            UpdateDailyWorkThread(services, teams, settingsService.GetSettingByKey<TimeSpan>("daily_work_thread"));
            UpdateWorkLogNotificationStart(services, settingsService.GetSettingByKey<TimeSpan>("work_log_notification_start"));
            UpdateWorkLogNotificationEnd(services, settingsService.GetSettingByKey<TimeSpan>("work_log_notification_end"));
            UserSync(services, settingsService);
            DaillyDayOffStateChange(services);
            CheckUsersPresence(services, settingsService.GetSettingByKey<int>("check_presence_interval"));
            SearchingAnomalies(services, settingsService.GetSettingByKey<int>("searching_anomalies_interval"));
        }
        public static void UpdateDailyWorkThread(IServiceProvider services, TeamsOptions teams, TimeSpan time)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate<ScheduledMessageService>(
                recurringJobId: "daily-work-thread",
                methodCall: job => job.PostWorkThreadAsync(teams.ChannelId, CancellationToken.None),
                cronExpression: Cron.Daily(time.Hours, time.Minutes),
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
            );
        }

        public static void UpdateWorkLogNotificationStart(IServiceProvider services, TimeSpan time)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate<WorkLogNotificationJob>(
                recurringJobId: "work-log-notification_start",
                methodCall: job => job.CheckWorkLogs("Start", null),
                cronExpression: Cron.Daily(time.Hours, time.Minutes),
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
            );
        }
        public static void UpdateWorkLogNotificationEnd(IServiceProvider services, TimeSpan time)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate<WorkLogNotificationJob>(
                recurringJobId: "work-log-notification_end",
                methodCall: job => job.CheckWorkLogs("End", null),
                cronExpression: Cron.Daily(time.Hours, time.Minutes),
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
            );
        }

        public static void DaillyDayOffStateChange(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.AddOrUpdate<IDayOffRequestService>(
                recurringJobId: "daily-day-off-state-change",
                methodCall: job => job.UpdateDayOffState(),
                cronExpression: Cron.Daily(0, 0),
                options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
            );
        }
        public static void UserSync(IServiceProvider services, ISettingsService settingsService)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            var isEnabled = settingsService.GetSettingByKey<bool>("user_sync_enabled");
            if (!isEnabled) return;

            var frequency = settingsService.GetSettingByKey<string>("user_sync_frequency")?.ToLower();
            var time = settingsService.GetSettingByKey<TimeSpan>("user_sync_time_of_day");

            string cron = frequency switch
            {
                "daily" => Cron.Daily(time.Hours, time.Minutes),
                "weekly" => GetWeeklyCrons(time, settingsService),
                "monthly" => GetMonthlyCron(time, settingsService),
                _ => Cron.Daily()
            };

            recurringJobManager.AddOrUpdate<IUserSyncService>(
                "user-sync-job",
                job => job.Sync(),
                cron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
            );
        }
        private static string GetWeeklyCrons(TimeSpan time, ISettingsService settingsService)
        {
            var days = settingsService.GetSettingByKey<string>("user_sync_days_of_week");
            var dayList = days?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(MapDayToCron)
                .Distinct()
                .OrderBy(d => d)
                .ToArray();

            if (dayList == null || dayList.Length == 0)
                dayList = ["1"]; // Monday

            var daysCron = string.Join(",", dayList);
            return $"{time.Minutes} {time.Hours} * * {daysCron}";
        }


        private static string MapDayToCron(string day)
        {
            return day.Trim().ToLower() switch
            {
                "sunday" => "0",
                "monday" => "1",
                "tuesday" => "2",
                "wednesday" => "3",
                "thursday" => "4",
                "friday" => "5",
                "saturday" => "6",
                _ => "1"
            };
        }
        private static string GetMonthlyCron(TimeSpan time, ISettingsService settingsService)
        {
            var dayOfMonth = settingsService.GetSettingByKey<int>("user_sync_day_of_month");
            if (dayOfMonth is < 1 or > 28 ) dayOfMonth = 1;

            return $"{time.Minutes} {time.Hours} {dayOfMonth} * *";
        }

        public static void CheckUsersPresence(IServiceProvider services, int intervalInMinutes)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate<PresensceManager>(
                "check-users-presence",
                job => job.CheckUsersPresence(),
                cronExpression: $"0/{intervalInMinutes} * * * *",
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
            );
        }

        public static void SearchingAnomalies(IServiceProvider services, int intervalInMinutes)
        {
            using var scope = services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.AddOrUpdate<IAnomalyWorklogService>(
                "searching-anomalies",
                job => job.CheckDailyWorkLogAnomalies(),
                cronExpression: $"0/{intervalInMinutes} * * * *",
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
            );
        }

    }
}

