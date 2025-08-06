using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using WorkflowTime.Configuration;

namespace WorkflowTime.Features.Teams.Bot.Services
{
    public class ScheduledMessageService
    {
        private readonly CloudAdapter _adapter;
        private readonly MicrosoftAppOptions _microsoftAppOptions;

        public ScheduledMessageService(CloudAdapter adapter, IOptions<MicrosoftAppOptions> _options)
        {
            _adapter = adapter;
            _microsoftAppOptions = _options.Value;
        }

        public async Task PostWorkThreadAsync(string teamId, CancellationToken cancellationToken = default)
        {
            var appId = _microsoftAppOptions.AppId;
            var serviceUrl = _microsoftAppOptions.ServiceUrl;
            var nextWorkDay = DateTime.UtcNow.AddDays(1);
            var title = $"Work in day {nextWorkDay:dd.MM.yyyy}";
            var conversationReference = new ConversationReference
            {
                ServiceUrl = serviceUrl,
                Conversation = new ConversationAccount(isGroup: true, id: teamId),
                Bot = new ChannelAccount(id: appId),
            };

            await _adapter.ContinueConversationAsync(appId, conversationReference, async (turnContext, ct) =>
            {
                await turnContext.SendActivityAsync(title, cancellationToken: ct);
            }, cancellationToken);
        }
    }
}
