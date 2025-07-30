using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using WorkflowTime.Configuration;
using WorkflowTime.Extensions;
using WorkflowTime.Features.AdminPanel.Services;

namespace WorkflowTime.Features.AdminPanel
{
    public class SettingUpdatedEventHandler : INotificationHandler<SettingUpdatedEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TeamsOptions _teamsOptions;
        private readonly ISettingsService _settingsService;
        private readonly IValidator<SettingUpdatedEvent> _validator;

        public SettingUpdatedEventHandler
        (
            IServiceProvider serviceProvider,
            ISettingsService settingsService,
            IOptions<TeamsOptions> teamsOptions,
            IValidator<SettingUpdatedEvent> validator
        )
        {
            _serviceProvider = serviceProvider;
            _teamsOptions = teamsOptions.Value;
            _settingsService = settingsService;
            _validator = validator;
        }

        public Task Handle(SettingUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var result = _validator.Validate(notification);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);

            switch (notification.Key)
            {
                case "daily_work_thread":
                    var time = TimeSpan.Parse(notification.Value!);
                    HangfireExtensions.UpdateDailyWorkThread(_serviceProvider, _teamsOptions, time);
                    break;

                case "work_log_notification_start":
                    var startTime = TimeSpan.Parse(notification.Value!);
                    HangfireExtensions.UpdateWorkLogNotificationStart(_serviceProvider, startTime);
                    break;

                case "work_log_notification_end":
                    var endTime = TimeSpan.Parse(notification.Value!);
                    HangfireExtensions.UpdateWorkLogNotificationEnd(_serviceProvider, endTime);
                    break;

                case "user_sync":
                    HangfireExtensions.UserSync(_serviceProvider, _settingsService);
                    break;
            }

            return Task.CompletedTask;
        }
    }

}
