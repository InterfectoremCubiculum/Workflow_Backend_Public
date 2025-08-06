using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using WorkflowTime.Features.Teams.Bot.Services.AI;
using WorkflowTime.Features.Teams.Bot.Services.Commands;

namespace WorkflowTime.Features.Teams.Bot
{
    public class BotHandler : TeamsActivityHandler
    {
        private readonly IWorkflowAnalyzer _workflowAnalyzer;
        private readonly WorkStateAiService _workStateAiService;
        private readonly WorkStateCommandService _workStateService;
        private readonly ILogger<BotHandler> _logger;

        public BotHandler(
            WorkStateCommandService workStateService,
            IWorkflowAnalyzer workflowAnalyzer,
            WorkStateAiService workStateAiService,
            ILogger<BotHandler> logger)
        {
            _workStateService = workStateService;
            _workflowAnalyzer = workflowAnalyzer;
            _workStateAiService = workStateAiService;
            _logger = logger;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Value is JObject jObject)
            {
                await HandleCommandObject(turnContext, jObject, cancellationToken);
                return;
            }

            await HandleTextMessage(turnContext, cancellationToken);
        }

        private async Task HandleCommandObject( ITurnContext<IMessageActivity> turnContext, JObject jObject, CancellationToken cancellationToken)
        {
            var type = jObject.Value<string>("type");
            var command = jObject.Value<string>("command");

            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(command))
            {
                _logger.LogWarning("Received invalid command format: {Command}", jObject.ToString());
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("Invalid command format."),
                    cancellationToken);
                return;
            }

            switch (type)
            {
                case "confirmWorkState":
                    await _workStateService.HandleConfirmationCommand(turnContext, command, cancellationToken);
                    break;
                case "workflow":
                    await _workStateService.HandleWorkStateCommands(turnContext, command, cancellationToken);
                    break;
            }
        }

        private async Task HandleTextMessage(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userMessage = turnContext.Activity.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(userMessage))
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("Please send a message or use a command."),
                    cancellationToken);
                return;
            }

            if (userMessage.StartsWith('/'))
            {
                var replyText = await _workStateService.HandleCmdCommand(userMessage, turnContext, cancellationToken);
                if (!string.IsNullOrEmpty(replyText))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(replyText), cancellationToken);
                }
                return;
            }

            await ProcessWorkflowMessage(turnContext, userMessage, cancellationToken);
        }

        private async Task ProcessWorkflowMessage( ITurnContext<IMessageActivity> turnContext, string userMessage, CancellationToken cancellationToken)
        {
            try
            {
                var action = await _workflowAnalyzer.AnalyzeWorkflow(userMessage, cancellationToken);
                var user = turnContext.Activity.From;
                var timestamp = turnContext.Activity.LocalTimestamp;

                var replyText = await _workStateAiService.HandleAiResponse(
                    Guid.Parse(user.AadObjectId),
                    action,
                    timestamp);
                var mention = new Mention
                {
                    Mentioned = user,
                    Text = $"<at>{user.Name}</at>"
                };
                var replyActivity = MessageFactory.Text($"{mention.Text} {replyText}");
                replyActivity.Entities = new List<Entity> { mention };

                await turnContext.SendActivityAsync(replyActivity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing workflow message");
                await turnContext.SendActivityAsync(
                    MessageFactory.Text($"❌ Something went wrong while processing your request: {ex.Message}"),
                    cancellationToken);
            }
        }
    }
}