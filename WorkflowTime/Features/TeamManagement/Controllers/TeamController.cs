using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Features.ProjectManagement.Dtos;
using WorkflowTime.Features.TeamManagement.Dtos;
using WorkflowTime.Features.TeamManagement.Queries;
using WorkflowTime.Features.TeamManagement.Services;

namespace WorkflowTime.Features.TeamManagement.Controllers
{
    [Authorize(Policy = "MinUserAccess")]
    [Route("api/[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;
        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        
        /// <summary>
        /// Creates a new team based on the provided parameters.
        /// </summary>
        /// <remarks>This method requires the caller to have administrator access, as enforced by the
        /// "OnlyAdministratorAccess" policy.</remarks>
        /// <param name="parameters">The details of the team to be created, including name, description, and other relevant information.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// with a <see cref="GetTeamDto"/> representing the newly created team.</returns>
        [Authorize(Policy = "OnlyAdministratorAccess")]
        [HttpPost]
        public async Task<ActionResult<GetTeamDto>> CreateTeam([FromBody] CreateTeamDto parameters)
        {
                var result = await _teamService.CreateTeam(parameters);
                return CreatedAtAction(nameof(GetTeam), new { id = result.Id }, result);
        }

        /// <summary>
        /// Retrieves the team details for the specified team identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the team to retrieve.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing the team details as a <see cref="GetProjectDto"/>.</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<GetProjectDto>> GetTeam(int id)
        {
            var result = await _teamService.GetTeam(id);
            return Ok(result);
        }

        /// <summary>
        /// Searches for teams based on the specified query parameters.
        /// </summary>
        /// <param name="parameters">The parameters used to filter and sort the search results. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="GetSearchedTeamDto"/> objects that match the search criteria.</returns>
        [HttpGet("Search")]
        public async Task<ActionResult<List<GetSearchedTeamDto>>> Search([FromQuery] TeamSearchQueryParameters parameters)
        {
            var result = await _teamService.Search(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a list of users associated with the specified team.
        /// </summary>
        /// <param name="teamId">The unique identifier of the team whose users are to be retrieved. Must be a positive integer.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// containing a list of <see cref="UsersInTeamDto"/> objects representing the users in the specified team.</returns>
        [HttpGet("{teamId:int}/Users")]
        public async Task<ActionResult<List<UsersInTeamDto>>> UsersInTeam(int teamId)
        {
            var result = await _teamService.UsersInTeam(teamId);
            return Ok(result);
        }
    }
}
