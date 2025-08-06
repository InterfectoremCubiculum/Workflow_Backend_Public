using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace WorkflowTime.Features.Teams.Bot.Services.Commands
{
    public class ConfirmationModal
    {
        public Task<Attachment> GenerateConfirmationCard(string type, string command, string text, ChannelAccount user)
        {
            var userMentionTag = $"<at>{user.Name}</at>";
            var fullText = $"{userMentionTag} {text}";

            var cardJson = new
            {
                type = "AdaptiveCard",
                version = "1.4",
                body = new object[]
                {
            new
            {
                type = "TextBlock",
                text = fullText,
                wrap = true,
                weight = "Bolder",
                size = "Medium"
            }
                },
                actions = new object[]
                {
            new
            {
                type = "Action.Submit",
                title = "Yes",
                data = new { type, command }
            },
            new
            {
                type = "Action.Submit",
                title = "No",
                data = new { type, command = "cancel" }
            }
                },
                schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                msteams = new
                {
                    entities = new object[]
                    {
                new
                {
                    type = "mention",
                    text = userMentionTag,
                    mentioned = new
                    {
                        id = user.AadObjectId,
                        name = user.Name
                    }
                }
                    }
                }
            };

            return Task.FromResult(new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.FromObject(cardJson)
            });
        }
    }
}
