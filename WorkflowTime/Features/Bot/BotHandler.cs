using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.Bot.Services;
using WorkflowTime.Features.Bot.Services.AI;
using Entity = Microsoft.Bot.Schema.Entity;
namespace WorkflowTime.Features.Bot
{
    public class BotHandler : TeamsActivityHandler
    {
        private readonly WorkStateCommnadService _workStateService;
        private readonly IWorkflowAnalyzer _workflowAnalyzer;
        private readonly WorkStateAiService _workStateAiService;
        private readonly ILogger<BotHandler> _logger;
        private readonly List<(string Title, string Description)> _commands =
        [
            ("help", "Shows help information"),
            ("echo", "Echoes back your message"),
            ("work", "Manage your work session")
        ];

        public BotHandler
        (
            WorkStateCommnadService workStateService,
            IWorkflowAnalyzer workflowAnalyzer,
            WorkStateAiService workStateAiService,
            ILogger<BotHandler> logger
        )
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
                var type = jObject.Value<string>("type");
                var command = jObject.Value<string>("command");

                if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(command))
                {
                    _logger.LogWarning("Received invalid command format: {Command}", jObject.ToString());
                    await turnContext.SendActivityAsync(MessageFactory.Text("Invalid command format."), cancellationToken);
                    return;
                }

                switch (type)
                {
                    case "confirmWorkState":
                        await HandleConfirmationCommand(turnContext, command, cancellationToken);
                        return;
                    case "workflow":
                        await HandleWorkStateCommands(turnContext, command, cancellationToken);
                        return;
                }
            }

            var userMessage = turnContext.Activity.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(userMessage))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Please send a message or use a command."), cancellationToken);
            }
            else if (userMessage.StartsWith('/'))
            {
                var textReply = await HandleCmdCommand(userMessage, turnContext, cancellationToken);
                if (!String.IsNullOrEmpty(textReply))
                    await turnContext.SendActivityAsync(MessageFactory.Text(textReply), cancellationToken);
            }
            else 
            {
                WorkflowActionResult action;
                try
                {
                    action = await _workflowAnalyzer.AnalyzeWorkflow(userMessage, cancellationToken);
                }
                catch (Exception ex)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"❌ Something went wrong while processing your request: {ex}"), cancellationToken);
                    return;
                }

                var user = turnContext.Activity.From;

                var time = turnContext.Activity.LocalTimestamp;

                var replyActivity = await _workStateAiService.HandleAiResponse(Guid.Parse(user.AadObjectId), action, time);
                await turnContext.SendActivityAsync(MessageFactory.Text(replyActivity), cancellationToken);
            }
        }


        private async Task HandleWorkStateCommands(ITurnContext turnContext, string commandObj, CancellationToken cancellationToken)
        {
            var command = commandObj.ToString()?.ToLowerInvariant();
            string reply;

            if (!Guid.TryParse(turnContext.Activity.From.AadObjectId, out var userId)) 
            { 
                _logger.LogWarning("Failed to parse user ID from activity: {Activity}", turnContext.Activity);
                await turnContext.SendActivityAsync(MessageFactory.Text("Failed to identify user."), cancellationToken);
            }

            switch (command)
            {
                case "start-work":
                    await _workStateService.StartWork(userId);
                    reply = "✅ Work Started!";
                    break;

                case "stop-work":
                    await _workStateService.StopWork(userId);
                    reply = "🛑 Work Endend.";
                    break;

                case "break-start":
                    await _workStateService.StartBreak(userId);
                    reply = "☕ Break Started.";
                    break;

                case "resume":
                    await _workStateService.ResumeWork(userId);
                    reply = "🔄 Resumed To Work.";
                    break;

                default:
                    reply = $"❌ WTF !??: {command}";
                    break;
            }

            await turnContext.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
            var updatedCard = await _workStateService.GenerateWorkCard(userId);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(updatedCard), cancellationToken);

            return;
        }

        private async Task<string?> HandleCmdCommand(string userMessage, ITurnContext turnContext,CancellationToken cancellationToken)
        {
            var parts = userMessage.Split(' ', 2);
            var cmd = parts[0][1..].ToLower();
            var args = parts.Length > 1 ? parts[1] : "";
            string textReply;
            switch (cmd)
            {
                case "help":
                    textReply = string.Join("\n", _commands.Select(c => $"/{c.Title} - {c.Description}"));
                    break;

                case "echo":
                    textReply = string.IsNullOrWhiteSpace(args) ? "(no message to echo)" : args;
                    break;
                case "work":
                    if (!Guid.TryParse(turnContext.Activity.From.AadObjectId, out var userId))
                    {
                        _logger.LogWarning("Failed to parse user ID from activity: {Activity}", turnContext.Activity);
                        await turnContext.SendActivityAsync(MessageFactory.Text("Failed to identify user."), cancellationToken);
                    }
                    var workCard = await _workStateService.GenerateWorkCard(userId);
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(workCard), cancellationToken);
                    return null;

                default:
                    textReply = "Unknown command. Type /help to see available commands.";
                    break;
            }

            return textReply;
        }

        private async Task HandleConfirmationCommand(ITurnContext turnContext, string command, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(turnContext.Activity.From.AadObjectId, out var userId))
            {
                _logger.LogWarning("Failed to parse user ID from activity: {Activity}", turnContext.Activity);
                await turnContext.SendActivityAsync(MessageFactory.Text("Failed to identify user."), cancellationToken);
            }
            string reply;

            var user = turnContext.Activity.From;

            var mention = new Mention
            {
                Mentioned = user,
                Text = $"<at>{user.Name}</at>"
            };

            try
            {
                switch (command)
                {
                    case "StartWork":
                        await _workStateService.StartWork(userId);
                        reply = "✅ Work started!";
                        break;
                    case "StartBreak":
                        await _workStateService.StartBreak(userId);
                        reply = "☕ Break started!";
                        break;
                    case "Resumework":
                        await _workStateService.ResumeWork(userId);
                        reply = "🔄 Back to work!";
                        break;
                    case "EndWork":
                        await _workStateService.StopWork(userId);
                        reply = "🛑 Work ended.";
                        break;
                    case "cancel":
                        reply = "❌ Action canceled.";
                        break;
                    default:
                        reply = "🤷 Unknown confirmation response.";
                        break;
                }
            }
            catch (ConflictException ex)
            {
                reply = $"⚠️ {ex.Message}";
            }
            catch (Exception)
            {
                reply = "❌ Something went wrong while processing your request. Please try again later.";
            }

            var messageText = $"{mention.Text} {reply}";
            var replyActivity = MessageFactory.Text(messageText);
            replyActivity.Entities = new List<Entity> { mention };

            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }
    }
}
