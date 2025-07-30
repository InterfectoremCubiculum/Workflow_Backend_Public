using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WorkflowTime.Database;
using WorkflowTime.Features.AdminPanel.Dtos;

namespace WorkflowTime.Features.AdminPanel.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly IMediator _mediator;

        public SettingsService(WorkflowTimeDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }
        public async Task<List<GetSettingDto>> GetSettings()
        {
            return await _dbContext.Settings
            .Where(s => !s.IsSystemSettings)
            .Select(s => new GetSettingDto
            {
                Key = s.Key,
                Value = s.Value,
                Type = s.Type,
                Description = s.Description,
                IsEditable = s.IsEditable
            })
            .ToListAsync();
        }

        public async Task UpdateSettings(List<UpdatedSettingDto> parameters)
        {
            var settings = await _dbContext.Settings
                .Where(s => parameters.Select(p => p.Key).Contains(s.Key))
                .ToListAsync();

            var updatedEvents = new List<SettingUpdatedEvent>();

            foreach (var setting in settings)
            {
                var updatedSetting = parameters.FirstOrDefault(p => p.Key == setting.Key);
                if (updatedSetting != null)
                {
                    setting.Value = updatedSetting.Value;
                    setting.UpdatedAt = DateTime.UtcNow;
                    updatedEvents.Add(new SettingUpdatedEvent(setting.Key, updatedSetting.Value));
                }
            }
            _dbContext.Settings.UpdateRange(settings);
            await _dbContext.SaveChangesAsync();

            foreach (var evt in updatedEvents)
            {
                await _mediator.Publish(evt);
            }
        }

        public T? GetSettingByKey<T>(string key)
        {
            var value = _dbContext.Settings
                .Where(s => s.Key == key)
                .Select(s => s.Value)
                .FirstOrDefault();

            if (value == null)
                return default;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            try
            {
                object parsed = targetType switch
                {
                    var t when t == typeof(string) => value,
                    var t when t == typeof(int) => int.Parse(value),
                    var t when t == typeof(decimal) => decimal.Parse(value, CultureInfo.InvariantCulture),
                    var t when t == typeof(bool) => bool.Parse(value),
                    var t when t == typeof(DateTime) => DateTime.Parse(value, CultureInfo.InvariantCulture),
                    var t when t == typeof(TimeSpan) => TimeSpan.Parse(value, CultureInfo.InvariantCulture),
                    var t when t.IsEnum => Enum.Parse(t, value),
                    _ => throw new NotSupportedException($"Unsupported type {targetType.Name}")
                };

                return (T)parsed;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert setting '{key}' value '{value}' to {typeof(T).Name}", ex);
            }
        }

        public async Task UpdateSettings_UserSync(List<UpdatedSettingDto> parameters)
        {
            var settings = await _dbContext.Settings
                .Where(s => parameters.Select(p => p.Key).Contains(s.Key))
                .ToListAsync();

            foreach (var setting in settings)
            {
                var updatedSetting = parameters.FirstOrDefault(p => p.Key == setting.Key);
                if (updatedSetting != null)
                {
                    setting.Value = updatedSetting.Value;
                    setting.UpdatedAt = DateTime.UtcNow;
                }
            }

            _dbContext.Settings.UpdateRange(settings);
            await _dbContext.SaveChangesAsync();
            await _mediator.Publish(new SettingUpdatedEvent("user_sync", null));
        }
    }
}
