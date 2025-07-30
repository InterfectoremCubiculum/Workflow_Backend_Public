using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using WorkflowTime.Enums;
using WorkflowTime.Features.DayOffs.Dtos;
using WorkflowTime.Features.DayOffs.Queries;
using WorkflowTime.Features.DayOffs.Services;
using WorkflowTime.Features.UserManagement.Services;
using WorkflowTime.Utillity;

namespace WorkflowTime.Features.DayOffs.Controllers
{
    /// <summary>
    /// Provides API endpoints for managing day off requests.
    /// </summary>
    [Authorize(Policy = "MinUserAccess")]
    [ApiController]
    [Route("api/[controller]")]
    public class DayOffRequestController : ControllerBase
    {
        private readonly IDayOffRequestService _dayOffRequestService;
        private readonly ICurrentUserService _currentUserService;
        public DayOffRequestController(IDayOffRequestService dayOffRequestService, ICurrentUserService currentUserService)
        {
            _dayOffRequestService = dayOffRequestService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Retrieves a paginated list of day-off requests based on the specified query parameters.
        /// </summary>
        /// <remarks>This method supports pagination and filtering of day-off requests. The query
        /// parameters determine the subset of requests returned.</remarks>
        /// <param name="parameters">The query parameters used to filter and paginate the day-off requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// with a <see cref="PagedResponse{T}"/> of <see cref="GetDayOffRequestDto"/> objects.</returns>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<GetDayOffRequestDto>>> GetDayOffRequests([FromQuery] DayOffsRequestQueryParameters parameters)
        {
            var result = await _dayOffRequestService.GetDayOffRequests(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a list of calendar day-off requests based on the specified query parameters.
        /// </summary>
        /// <param name="parameters">The query parameters used to filter the day-off requests.</param>
        /// <returns>A list of <see cref="GetCalendarDayOff"/> objects that match the query parameters. Returns a 404 status if
        /// no matching requests are found.</returns>
        [HttpGet("calendar/")]
        public async Task<ActionResult<List<GetCalendarDayOff>>> GetCalendarDayOffRequests([FromQuery] CalendarDayOffsRequestQueryParameters parameters)
        {
            var result = await _dayOffRequestService.GetCalendarDayOff(parameters);
            if (result == null)
            {
                return NotFound($"Day off request with user ID {User} not found.");
            }
            return Ok(result);
        }

        /// <summary>
        /// Creates a new day off request for the current user.
        /// </summary>
        /// <remarks>This method handles HTTP POST requests to create a new day off request. If the
        /// creation is successful, it returns a 201 Created response with the details of the created request. If the
        /// creation fails, it returns a 400 Bad Request response.</remarks>
        /// <param name="dayOffRequest">The details of the day off request to be created.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// with a <see cref="GetCreatedDayOffRequestDto"/> representing the created day off request.</returns>
        [HttpPost]
        public async Task<ActionResult<GetCreatedDayOffRequestDto>> CreateDayOffRequest([FromBody] CreateDayOffRequestDto dayOffRequest)
        {

            var result = await _dayOffRequestService.CreateDayOffRequest(dayOffRequest, _currentUserService.UserId);
            if (result == null)
            {
                return BadRequest("Failed to create day off request.");
            }
            return CreatedAtAction(nameof(GetDayOffRequests), new { id = result.Id }, result);
        }

        /// <summary>
        /// Updates the status of a day off request.
        /// </summary>
        /// <remarks>This method applies a partial update to the day off request identified by <paramref
        /// name="id"/>. It sets the request's status to the specified <paramref name="status"/> and returns a response
        /// with no content if the update is successful.</remarks>
        /// <param name="id">The unique identifier of the day off request to update.</param>
        /// <param name="status">The new status to set for the day off request.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        [HttpPatch("{id:int}/{status}")]
        public async Task<IActionResult> UpdateDayOffRequestStatus(int id, DayOffRequestStatus status)
        {
            await _dayOffRequestService.UpdateDayOffRequestStatus(id, status);
            return NoContent();

        }

        /// <summary>
        /// Updates an existing day off request with the specified details.
        /// </summary>
        /// <remarks>This method updates the day off request in the system using the provided details.
        /// Ensure that the request identifier is valid and that the user has permission to update the
        /// request.</remarks>
        /// <param name="dayOffRequest">The details of the day off request to update. This must include the request identifier and any changes to be
        /// applied.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the update operation. Returns <see
        /// cref="NoContentResult"/> if the update is successful.</returns>
        [HttpPut]
        public async Task<IActionResult> UpdateDayOffRequest([FromBody] UpdateDayOffRequestDto dayOffRequest)
        {
            await _dayOffRequestService.UpdateDayOffRequest(dayOffRequest);
            return NoContent();
        }

        /// <summary>
        /// Deletes a day off request identified by the specified ID.
        /// </summary>
        /// <remarks>This method performs an HTTP DELETE operation to remove (soft delete) a day off request. Ensure
        /// that the request ID exists before calling this method to avoid errors.</remarks>
        /// <param name="id">The unique identifier of the day off request to delete. Must be a positive integer.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="NoContentResult"/>
        /// if the deletion is successful.</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDayOffRequest(int id)
        {
            await _dayOffRequestService.DeleteDayOffRequest(id);
            return NoContent();
        }
    }
}
