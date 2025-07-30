using MediatR;

namespace WorkflowTime.Features.AdminPanel
{
    public class SettingUpdatedEvent : INotification
    {
        public string Key { get; }
        public string? Value { get; }

        public SettingUpdatedEvent(string key, string? value)
        {
            Key = key;
            Value = value;
        }
    }

}
