using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.UserManagement.Services;

namespace WorkflowTime.Features.Notifications.Controllers
{
    /// <summary>
    /// Provides endpoints for managing user notifications.
    /// </summary>
    /// <remarks>This controller allows users to retrieve and update the status of their notifications. It
    /// requires the user to have at least the minimum user access policy defined by the application.</remarks>
    [Authorize(Policy = "MinUserAccess")]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {

        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        
        public NotificationController(INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Retrieves a list of notifications for the current user.
        /// </summary>
        /// <remarks>This method uses the current user's ID to fetch notifications. The result is filtered
        /// based on the <paramref name="read"/> parameter to include either read or unread notifications.</remarks>
        /// <param name="read">Specifies whether to include notifications that have been read. If <see langword="true"/>, only read
        /// notifications are included; otherwise, unread notifications are included.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// of type <see cref="List{T}"/> containing <see cref="SendedNotificationDto"/> objects.</returns>
        [HttpGet]
        public async Task<ActionResult<List<SendedNotificationDto>>> GetNotifications([FromQuery] bool read = true)
        {
            var data = await _notificationService.GetNotifications(_currentUserService.UserId, read);
            return Ok(data);
        }

        /// <summary>
        /// Marks the specified notifications as read. (Soft Delete)
        /// </summary>
        /// <param name="notificationsIds">A list of notification IDs to be marked as read. Cannot be null or empty.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="NoContentResult"/>
        /// if successful.</returns>
        [HttpPatch("markAsRead")]
        public async Task<IActionResult> MarkNotificationsAsRead([FromBody] List<int> notificationsIds)
        {
            await _notificationService.MarkNotifications(notificationsIds);
            return NoContent();
        }
    }
}
