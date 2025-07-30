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
            "user_sync"
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

                default:
                    return false;
            }
        }
    }

}
