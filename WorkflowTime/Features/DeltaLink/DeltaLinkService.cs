using Microsoft.EntityFrameworkCore;
using WorkflowTime.Database;

namespace WorkflowTime.Features.DeltaLink
{
    public class DeltaLinkService
    {
        private readonly WorkflowTimeDbContext _dbContext;

        public DeltaLinkService(WorkflowTimeDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<string?> GetDeltaLink()
        {
            return await _dbContext.Settings
                .Where(s => s.Key == "GraphDeltaLink")
                .Select(s => s.Value)
                .FirstOrDefaultAsync();
        }

        public async Task SaveDeltaLink(string deltaLink)
        {
            var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == "GraphDeltaLink");

            if (setting == null)
            {
                setting = new SettingModel
                {
                    Key = "GraphDeltaLink",
                    Value = deltaLink,
                    IsSystemSettings = true,
                    IsEditable = false,
                    Type = Enums.SettingsType.String,
                };
                _dbContext.Settings.Add(setting);
            }
            else
            {
                setting.Value = deltaLink;
                setting.UpdatedAt = DateTime.UtcNow;
                _dbContext.Settings.Update(setting);
            }

            await _dbContext.SaveChangesAsync();
        }

    }
}
