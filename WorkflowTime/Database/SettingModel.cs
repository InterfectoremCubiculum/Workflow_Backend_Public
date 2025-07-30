using System.ComponentModel.DataAnnotations;
using WorkflowTime.Enums;

namespace WorkflowTime.Database
{
    public class SettingModel
    {
        [Key]
        public required string Key { get; set; }
        public required string Value { get; set; }
        public SettingsType Type { get; set; }
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsEditable { get; set; } = true;
        public bool IsSystemSettings { get; set; } = false;
    }

}
