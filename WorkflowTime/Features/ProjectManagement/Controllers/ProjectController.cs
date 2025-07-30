using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Features.ProjectManagement.Dtos;
using WorkflowTime.Features.ProjectManagement.Queries;
using WorkflowTime.Features.ProjectManagement.Services;

namespace WorkflowTime.Features.ProjectManagement.Controllers
{
    [Authorize(Policy = "MinUserAccess")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Creates a new project based on the provided project data.
        /// </summary>
        /// <remarks>This method requires the caller to have administrator access as specified by the
        /// "OnlyAdministratorAccess" policy.</remarks>
        /// <param name="projectDto">The data transfer object containing the details of the project to be created.</param>
        /// <returns>An <see cref="ActionResult"/> representing the result of the action. If successful, returns a 201 Created
        /// response with the location of the newly created project.</returns>
        [Authorize(Policy = "OnlyAdministratorAccess")]
        [HttpPost]
        public async Task<ActionResult> CreateProject([FromBody] CreateProjectDto projectDto)
        {

            var result = await _projectService.CreateProject(projectDto);
            return CreatedAtAction(nameof(GetProject), new { id = result.Id }, result);
        }

        /// <summary>
        /// Retrieves the project details for the specified project identifier.
        /// </summary>
        /// <remarks>This method sends an HTTP GET request to retrieve the project details. Ensure that
        /// the project with the specified <paramref name="id"/> exists.</remarks>
        /// <param name="id">The unique identifier of the project to retrieve. Must be a positive integer.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="GetProjectDto"/> with the project details if found;
        /// otherwise, a not found result.</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<GetProjectDto>> GetProject(int id)
        {
            var result = await _projectService.GetProject(id);
            return Ok(result);
        }

        /// <summary>
        /// Searches for projects based on the specified query parameters.
        /// </summary>
        /// <param name="parameters">The parameters used to filter and sort the search results. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// containing a list of <see cref="GetSearchedProjectDto"/> objects that match the search criteria.</returns>
        [HttpGet("Search")]
        public async Task<ActionResult<List<GetSearchedProjectDto>>> Search([FromQuery] ProjectSearchQueryParameters parameters)
        {
            var result = await _projectService.Search(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a list of users associated with the specified project.
        /// </summary>
        /// <remarks>This method sends an HTTP GET request to retrieve users associated with a given
        /// project ID. The response includes a list of users in the form of data transfer objects.</remarks>
        /// <param name="projectId">The unique identifier of the project for which to retrieve users.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ActionResult{T}"/>
        /// containing a list of <see cref="UsersInProjectDto"/> objects representing the users in the specified
        /// project.</returns>
        [HttpGet("{projectId:int}/Users")]
        public async Task<ActionResult<List<UsersInProjectDto>>> UsersInProject(int projectId)
        {
            var result = await _projectService.UsersInProject(projectId);
            return Ok(result);
        }

    }
}
