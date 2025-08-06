using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder;
using Microsoft.AspNetCore.Authorization;

namespace WorkflowTime.Features.Teams.Bot.Controllers
{
    /// <summary>
    /// Provides API endpoints for interacting with the bot service.
    /// </summary>
    /// <remarks>This controller handles HTTP POST requests to process bot activities. It uses a <see
    /// cref="CloudAdapter"/> to manage the communication between the bot and the client, and an <see cref="IBot"/>
    /// instance to handle the bot logic. The controller is configured to allow anonymous access.</remarks>
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly CloudAdapter _adapter;
        private readonly IBot _bot;

        public BotController(CloudAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        /// <summary>
        /// Processes an HTTP POST request asynchronously.
        /// </summary>
        /// <remarks>This method handles incoming HTTP POST requests by processing them with the specified
        /// bot adapter. It returns a 204 No Content response upon successful processing.</remarks>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation. Returns <see
        /// cref="NoContentResult"/> if the request is processed successfully.</returns>
        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            await _adapter.ProcessAsync(Request, Response, _bot);
            return NoContent();
        }
    }
}
