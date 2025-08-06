
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Polly;
using Polly.Registry;
using WorkflowTime.Configuration;
using WorkflowTime.Database;
using WorkflowTime.Enums;

namespace WorkflowTime.Features.Notifications.Services
{
    /// <summary>
    /// Teams notification service.
    /// </summary>
    public class NotificationTeamsService : INotificationTeamsService
    {
        private readonly ResiliencePipeline _notifyTeamsPipeLine;
        private readonly CloudAdapter _adapter;
        private readonly ILogger<NotificationTeamsService> _logger;
        private readonly WorkflowTimeDbContext _dbContext;

        private readonly string _appId;
        private readonly string _serviceUrl;
        private readonly string _tenantId;
        public NotificationTeamsService
        (
            IOptions<AzureAdOptions> azureAdOptions,
            IOptions<MicrosoftAppOptions> microsoftAppOptions,
            ResiliencePipelineProvider<string> pipelineProvider,
            CloudAdapter adapter,
            ILogger<NotificationTeamsService> logger,
            WorkflowTimeDbContext dbContext

        )
        {
            _notifyTeamsPipeLine = pipelineProvider.GetPipeline("TeamsNotificationPipeLine");
            _appId = microsoftAppOptions.Value.AppId;
            _serviceUrl = microsoftAppOptions.Value.ServiceUrl;
            _tenantId = azureAdOptions.Value.TenantId;
            _adapter = adapter;
            _logger = logger;
            _dbContext = dbContext;
        }
        public async Task SendNotification(Guid userId, string messageToSend)
        {
            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Bot = new ChannelAccount(_appId),
                Members = new List<ChannelAccount>
                {
                        new ChannelAccount(id:$"{userId.ToString()}")
                },
                TenantId = _tenantId,
                ChannelData = new { tenant = new { id = _tenantId } }
            };

            try
            {
                await _notifyTeamsPipeLine.ExecuteAsync(async ct =>
                {
                    await _adapter.CreateConversationAsync(
                        _appId,
                        "msteams",
                        _serviceUrl,
                        null,
                        conversationParameters,
                        async (turnContext, innerCt) =>
                        {
                            await turnContext.SendActivityAsync(messageToSend, cancellationToken: innerCt);
                        },
                        ct);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to user {userId}");
            }
        }
        public async Task SendNotification(UserRole? userToInformRole, string messageToSend)
        {
            var userIds = await _dbContext.Users
                .Where(u => u.Role == userToInformRole && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();

            var tasks = userIds.Select(userId => SendNotification(userId, messageToSend));
            await Task.WhenAll(tasks);
        }
    }
}
