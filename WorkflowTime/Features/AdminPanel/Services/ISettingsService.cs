using WorkflowTime.Enums;
using WorkflowTime.Features.AdminPanel.Dtos;

namespace WorkflowTime.Features.AdminPanel.Services
{
    public interface ISettingsService
    {
        public Task UpdateSettings(List<UpdatedSettingDto> parameters);
        public Task UpdateSettings_UserSync(List<UpdatedSettingDto> parameters);
        public Task<List<GetSettingDto>> GetSettings();
        public T? GetSettingByKey<T>(string key);
    }
}
