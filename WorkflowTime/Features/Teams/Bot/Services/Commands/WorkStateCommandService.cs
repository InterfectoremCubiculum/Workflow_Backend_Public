using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using WorkflowTime.Enums;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.WorkLog.Services;

namespace WorkflowTime.Features.Teams.Bot.Services.Commands
{
    public class WorkStateCommandService
    {
        private readonly IWorkLogService _workLogService;
        private readonly INotificationService _hub;
        private readonly ILogger<WorkStateCommandService> _logger;
        private const string Type = "workflow";
        private readonly List<(string Title, string Description)> _commands =
        [
            ("help", "Shows help information"),
            ("echo", "Echoes back your message"),
            ("work", "Manage your work session")
        ];
        public WorkStateCommandService(IWorkLogService workLogService, INotificationService hub, ILogger<WorkStateCommandService> logger)
        {
            _logger = logger;
            _workLogService = workLogService;
            _hub = hub;
        }


        public async Task HandleWorkStateCommands(ITurnContext turnContext, string commandObj, CancellationToken cancellationToken)
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
                    await StartWork(userId);
                    reply = "✅ Work Started!";
                    break;

                case "stop-work":
                    await StopWork(userId);
                    reply = "🛑 Work Endend.";
                    break;

                case "break-start":
                    await StartBreak(userId);
                    reply = "☕ Break Started.";
                    break;

                case "resume":
                    await ResumeWork(userId);
                    reply = "🔄 Resumed To Work.";
                    break;

                default:
                    reply = $"❌ WTF !??: {command}";
                    break;
            }

            await turnContext.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
            var updatedCard = await GenerateWorkCard(userId);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(updatedCard), cancellationToken);

            return;
        }

        public async Task<string?> HandleCmdCommand(string userMessage, ITurnContext turnContext, CancellationToken cancellationToken)
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
                    var workCard = await GenerateWorkCard(userId);
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(workCard), cancellationToken);
                    return null;

                default:
                    textReply = "Unknown command. Type /help to see available commands.";
                    break;
            }

            return textReply;
        }

        public async Task HandleConfirmationCommand(ITurnContext turnContext, string command, CancellationToken cancellationToken)
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
                        await StartWork(userId);
                        reply = "✅ Work started!";
                        break;
                    case "StartBreak":
                        await StartBreak(userId);
                        reply = "☕ Break started!";
                        break;
                    case "Resumework":
                        await ResumeWork(userId);
                        reply = "🔄 Back to work!";
                        break;
                    case "EndWork":
                        await StopWork(userId);
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

        private async Task<Attachment> GenerateWorkCard(Guid userId)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 4))
            {
                Body = 
                {
                    new AdaptiveTextBlock
                    {
                        Text = "What's Your Plans ?",
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Medium
                    }
                },
                Actions = new List<AdaptiveAction>()
            };

            string userStatus = "idle";
            await _workLogService.GetUserActiveTimeSegmentType(userId).ContinueWith(task =>
            {
                userStatus = task.Result switch
                {
                    TimeSegmentType.Work => "working",
                    TimeSegmentType.Break => "break",
                    _ => "idle"
                };
            });

            switch (userStatus)
            {
                case "idle":
                    card.Actions.Add(new AdaptiveSubmitAction
                    {
                        Title = "▶️ Start Work",
                        Data = new { type = Type, command = "start-work" }
                    });
                    break;
                case "working":
                    card.Actions.Add(new AdaptiveSubmitAction
                    {
                        Title = "☕ Start Break",
                        Data = new { type = Type, command = "break-start" }
                    });
                    card.Actions.Add(new AdaptiveSubmitAction
                    {
                        Title = "🛑 End Work",
                        Data = new { type = Type, command = "stop-work" }
                    });
                    break;
                case "break":
                    card.Actions.Add(new AdaptiveSubmitAction
                    {
                        Title = "🔄 Go Back To Work",
                        Data = new { type = Type, command = "resume" }
                    });
                    card.Actions.Add(new AdaptiveSubmitAction
                    {
                        Title = "🛑 End Work",
                        Data = new { type = Type, command = "stop-work" }
                    });
                    break;
            }

            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = card
            };
        }

        private async Task StartWork(Guid userId)
        {
            await _workLogService.StartWork(userId, null);
            await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Work.ToString());
        }

        private async Task StopWork(Guid userId)
        {
            await _workLogService.EndWork(userId, null);
            await _hub.NotifyWorkStateChange(userId, null);
        }

        private async Task StartBreak(Guid userId)
        {
            await _workLogService.StartBreak(userId, null);
            await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Break.ToString());
        }

        private async Task ResumeWork(Guid userId)
        {
            await _workLogService.ResumeWork(userId, null);
            await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Work.ToString());
        }
    }
}
