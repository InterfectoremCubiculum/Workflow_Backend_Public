using WorkflowTime.Enums;

namespace WorkflowTime.Features.AdminPanel.Dtos
{
    public class GetSettingDto
    {
        public required string Key { get; set; }
        public required string Value { get; set; }
        public SettingsType Type { get; set; }
        public string? Description { get; set; }
        public bool IsEditable { get; set; } = true;
    }
}
