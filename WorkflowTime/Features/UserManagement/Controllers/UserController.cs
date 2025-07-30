using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using WorkflowTime.Features.UserManagement.Dtos;
using WorkflowTime.Features.UserManagement.Queries;
using WorkflowTime.Features.UserManagement.Services;

namespace WorkflowTime.Features.UserManagement.Controllers
{
    [Authorize(Policy = "MinUserAccess")]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IUserService _userService;
        private readonly GraphServiceClient _graphClient;
        private readonly IUserSyncService _userSyncService;

        public UserController
        (
            ITokenAcquisition tokenAcquisition,
            IUserService userSerivce,
            GraphServiceClient graphClient,
            IUserSyncService userSyncService
        )
        {
            _tokenAcquisition = tokenAcquisition;
            _userService = userSerivce;
            _graphClient = graphClient;
            _userSyncService = userSyncService;
        }

        /// <summary>
        /// Initiates a synchronization process for user data.
        /// </summary>
        /// <remarks>This method triggers the user synchronization service to update user data.  It
        /// requires the caller to have administrator access as specified by the "OnlyAdministratorAccess"
        /// policy.</remarks>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the synchronization operation.  Returns <see
        /// cref="NoContentResult"/> if the operation completes successfully.</returns>
        [Authorize(Policy = "OnlyAdministratorAccess")]
        [HttpGet("Sync")]
        public async Task<ActionResult<GetUserDto>> Sync()
        {
            await _userSyncService.Sync();
            return NoContent();
        }

        /// <summary>
        /// Retrieves the current user's information.
        /// </summary>
        /// <remarks>This method is an HTTP GET endpoint that returns the information of the authenticated
        /// user.</remarks>
        /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="GetMeDto"/> object with the current user's details.</returns>
        [HttpGet("Me")]
        public async Task<ActionResult<GetMeDto>> GetMe()
        {
            var result = await _userService.GetMe();
            return Ok(result);
        }

        /// <summary>
        /// Searches for users based on the specified query parameters.
        /// </summary>
        /// <param name="parameters">The parameters used to filter and search for users. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="GetSearchedUserDto"/> objects that match the search criteria.</returns>
        [HttpGet("Search")]
        public async Task<ActionResult<List<GetSearchedUserDto>>> Search([FromQuery] UserSearchQueryParameters parameters)
        {
            var result = await _userService.Search(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a list of users based on their unique identifiers.
        /// </summary>
        /// <remarks>This method sends a POST request to the "GetUsersByGuids" endpoint. It is designed to
        /// handle multiple user identifiers and return the corresponding user details.</remarks>
        /// <param name="userIds">A list of unique identifiers representing the users to retrieve. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// containing a list of <see cref="GetUsersByGuidDto"/> objects representing the users.</returns>
        [HttpPost("GetUsersByGuids")]
        public async Task<ActionResult<List<GetUsersByGuidDto>>> GetUsersByGuids(List<Guid> userIds)
        {
            var result = await _userService.GetUsersByGuids(userIds);
            return Ok(result);
        }


        // Test
        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(["User.Read"]);

            var user = await _graphClient.Me.GetAsync();
            return Ok(user);
        }
    }
}
