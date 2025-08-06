using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Enums;
using WorkflowTime.Features.UserManagement.Services;
using WorkflowTime.Features.WorkLog.Dtos;
using WorkflowTime.Features.WorkLog.Queries;
using WorkflowTime.Features.WorkLog.Services;
using WorkflowTime.Queries;

namespace WorkflowTime.Features.WorkLog.Controllers
{

    [Authorize(Policy = "MinUserAccess")]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkLogController : ControllerBase
    {
        private readonly IWorkLogService _workLogService;
        private readonly ICurrentUserService _currentUserService;
        public WorkLogController(IWorkLogService workLogService, ICurrentUserService currentUserService)
        {
            _workLogService = workLogService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Initiates a work session for the current user.
        /// </summary>
        /// <remarks>This method starts a new work session for the user identified by the current context.
        /// It logs the start of the work session and returns a response indicating the action was successful.</remarks>
        /// <returns>An <see cref="IActionResult"/> indicating that the work session has been successfully started.</returns>
        [HttpPost("StartWork")]
        public async Task<IActionResult> StartWork()
        {
            await _workLogService.StartWork(_currentUserService.UserId, null);
            return CreatedAtAction(nameof(StartWork), "Work started.");
        }

        /// <summary>
        /// Ends the current work session for the authenticated user.
        /// </summary>
        /// <remarks>This method logs the end of a work session for the user identified by the current
        /// authentication context. It returns a confirmation message upon successful completion.</remarks>
        /// <returns>An <see cref="ActionResult{T}"/> containing a confirmation message indicating that the work session has
        /// ended.</returns>
        [HttpPost("EndWork")]
        public async Task<ActionResult<string>> EndWork()
        {
            await _workLogService.EndWork(_currentUserService.UserId, null);
            return Ok("Work ended.");
        }
        /// <summary>
        /// Initiates a break for the current user.
        /// </summary>
        /// <remarks>This method records the start of a break period for the user identified by the
        /// current session. It returns a confirmation message upon successful initiation of the break.</remarks>
        /// <returns>An <see cref="ActionResult{T}"/> containing a confirmation message indicating that the break has started.</returns>
        [HttpPost("StartBreak")]
        public async Task<ActionResult<string>> StartBreak()
        {
            await _workLogService.StartBreak(_currentUserService.UserId, null);
            return Ok("Break started.");
        }

        /// <summary>
        /// Resumes the work for the current user.
        /// </summary>
        /// <remarks>This method resumes the work session for the user identified by the current user
        /// service. It sends a request to the work log service to continue the user's work and returns a confirmation
        /// message.</remarks>
        /// <returns>An <see cref="ActionResult{T}"/> containing a confirmation message indicating that the work has been
        /// resumed.</returns>
        [HttpPost("ResumeWork")]
        public async Task<ActionResult<string>> ResumeWork()
        {
            await _workLogService.ResumeWork(_currentUserService.UserId, null);
            return Ok("Work resumed.");
        }

        /// <summary>
        /// Retrieves the work log entries for a specified user based on the provided query parameters.
        /// </summary>
        /// <remarks>This method supports HTTP GET requests and retrieves work log entries for a user. If
        /// the user ID is not specified in the query parameters, the method defaults to using the ID of the currently
        /// authenticated user.</remarks>
        /// <param name="parameters">The query parameters used to filter the work log entries. If <paramref name="parameters.UserId"/> is null,
        /// the current user's ID is used.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="UsersTimeSegmentDto"/> objects representing
        /// the user's work log entries.</returns>
        [HttpGet("ByUser")]
        public async Task<ActionResult<List<UsersTimeSegmentDto>>> GetUserWorkLog([FromQuery] UserWorkLogQueryParameters parameters)
        {
            if (parameters.UserId == null)
                parameters.UserId = _currentUserService.UserId;

            var result = await _workLogService.GetUserWorkLog(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a list of worklog entries for users based on the specified query parameters.
        /// </summary>
        /// <remarks>This method requires the caller to have administrator access, as enforced by the
        /// "OnlyAdministratorAccess" policy.</remarks>
        /// <param name="parameters">The parameters used to filter and query the user timeline worklogs.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="UsersTimelineWorklogDto"/> objects representing the worklog entries.</returns>
        [Authorize(Policy = "OnlyAdministratorAccess")]
        [HttpPost("UserTimeline")]
        public async Task<ActionResult<List<UsersTimelineWorklogDto>>> UsersTimelineWorklog([FromBody] UserTimelineWorklogQueryParameters parameters)
        {
            var result = await _workLogService.UsersTimelineWorklog(parameters);
            return Ok(result);
        }
        /// <summary>
        /// Retrieves the project timeline worklog based on the specified query parameters.
        /// </summary>
        /// <remarks>This method requires the caller to have administrator access as specified by the
        /// "OnlyAdministratorAccess" policy.</remarks>
        /// <param name="parameters">The parameters used to query the project timeline worklog. This must include valid criteria for filtering the
        /// worklog data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/> of type
        /// <see cref="ProjectInTimeLineDto"/> representing the project timeline worklog.</returns>
        [Authorize(Policy = "OnlyAdministratorAccess")]
        [HttpPost("ProjectTimeline")]
        public async Task<ActionResult<ProjectInTimeLineDto>> ProjectTimelineWorklog([FromBody] GroupTimelineWorklogQueryParameters parameters)
        {
            var result = await _workLogService.ProjectTimelineWorklog(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the worklog timeline for a specified team based on the provided query parameters.
        /// </summary>
        /// <remarks>This method requires administrator access and is protected by the
        /// "OnlyAdministratorAccess" policy.</remarks>
        /// <param name="parameters">The parameters used to filter and define the timeline worklog for the team. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see
        /// cref="TeamInTimeLineDto"/> object representing the team's worklog timeline.</returns>
        [Authorize(Policy = "OnlyAdministratorAccess")]
        [HttpPost("TeamTimeLine")]
        public async Task<ActionResult<TeamInTimeLineDto>> TeamTimelineWorklog([FromBody] GroupTimelineWorklogQueryParameters parameters)
        {
            var result = await _workLogService.TeamTimelineWorklog(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Initiates a synchronization process for widgets associated with the current user.
        /// </summary>
        /// <remarks>This method retrieves the widget synchronization data for the user identified by the
        /// current session. It returns the data encapsulated in a <see cref="WidgetSyncDto"/> object.</remarks>
        /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="WidgetSyncDto"/> with the synchronization details.</returns>
        [HttpGet("WidgetSync")]
        public async Task<ActionResult<WidgetSyncDto>> WidgetSync()
        {
            var result = await _workLogService.WidgetSync(_currentUserService.UserId);
            return Ok(result);
        }

        [Authorize(Policy = "OnlyAdministratorAccess")]
        [HttpPut("ResolveActionRequest")]
        public async Task<IActionResult> ResolveActionRequest([FromQuery] int timeSegmentId, [FromBody] ResolveActionCommand action)
        {
            await _workLogService.ResolveActionRequest(timeSegmentId, action);
            return Ok("Action request resolved successfully.");
        }
    }
}
