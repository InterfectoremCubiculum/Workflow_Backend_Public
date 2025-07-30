using AdaptiveCards;
using Microsoft.Bot.Schema;
using WorkflowTime.Enums;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.WorkLog.Services;

namespace WorkflowTime.Features.Bot.Services
{
    public class WorkStateCommnadService
    {
        private readonly IWorkLogService _workLogService;
        private readonly INotificationService _hub;
        private const string Type = "workflow";

        public WorkStateCommnadService(IWorkLogService workLogService, INotificationService hub)
        {
            _workLogService = workLogService;
            _hub = hub;
        }

        public async Task<Attachment> GenerateWorkCard(Guid userId)
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
                    break;
            }

            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = card
            };
        }

        public async Task StartWork(Guid userId)
        {
            await _workLogService.StartWork(userId, null);
            await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Work.ToString());
        }

        public async Task StopWork(Guid userId)
        {
            await _workLogService.EndWork(userId, null);
            await _hub.NotifyWorkStateChange(userId, null);
        }

        public async Task StartBreak(Guid userId)
        {
            await _workLogService.StartBreak(userId, null);
            await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Break.ToString());
        }

        public async Task ResumeWork(Guid userId)
        {
            await _workLogService.ResumeWork(userId, null);
            await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Work.ToString());
        }
    }
}
