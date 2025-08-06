using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;

namespace WorkflowTime.Features.Teams.Bot
{
    public class AdapterWithErrorHandler : CloudAdapter
    {
        public AdapterWithErrorHandler(BotFrameworkAuthentication auth, ILogger<CloudAdapter> logger): base(auth, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                logger.LogError("Exception caught: {ExceptionMessage}", exception.Message);
                await turnContext.SendActivityAsync("Sorry, something went wrong.");
            };
        }
    }
}
