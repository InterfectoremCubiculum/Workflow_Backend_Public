namespace WorkflowTime.Features.AdminPanel
{
    using FluentValidation;

    public class SettingUpdatedEventValidator : AbstractValidator<SettingUpdatedEvent>
    {
        private static readonly string[] AllowedKeys =
        {
            "daily_work_thread",
            "work_log_notification_start",
            "work_log_notification_end",
            "user_sync",
            "max_time_away",
            "max_time_break",
            "max_work_time",
            "time_away_when_user_get_notification",
            "max_reverse_registration_time_logged",
            "max_reverse_registration_time",
            "max_summarise_break_time"
        };

        public SettingUpdatedEventValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .Must(key => AllowedKeys.Contains(key))
                .WithMessage("Invalid setting key.");

            RuleFor(x => x.Value)
                .Must((x, value) => IsValidForKey(x.Key, value))
                .WithMessage(x => $"Invalid value for key '{x.Key}'.");
        }

        private static bool IsValidForKey(string key, string value)
        {
            switch (key)
            {
                case "daily_work_thread":
                case "work_log_notification_start":
                case "work_log_notification_end":
                    return !string.IsNullOrEmpty(value) && TimeSpan.TryParse(value, out _);

                case "user_sync":
                    return true;

                case "max_time_away":
                case "max_time_break":
                case "max_work_time":
                case "time_away_when_user_get_notification":
                case "max_reverse_registration_time_logged":
                case "max_reverse_registration_time":
                case "max_summarise_break_time":
                    return !string.IsNullOrEmpty(value) && int.TryParse(value, out _);
                default:
                    return false;
            }
        }
    }

}
