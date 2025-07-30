using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Features.Summary.Queries;
using WorkflowTime.Features.Summary.Services;

namespace WorkflowTime.Features.Summary.Controllers
{
    [ApiController()]
    [Route("api/[controller]")]
    [Authorize(Policy = "OnlyAdministratorAccess")]
    public class SummaryController : ControllerBase
    {
        private readonly ISummaryService _summaryService;
        private readonly ISummaryCached _summaryCached;
        public SummaryController(ISummaryService summariseService, ISummaryCached summaryCached)
        {
            _summaryService = summariseService;
            _summaryCached = summaryCached;
        }

        /// <summary>
        /// Retrieves a summary of work data for specified users.
        /// </summary>
        /// <remarks>The method returns aobject with the work summary and a cache token. The cache
        /// token can be used for subsequent operations that require cached data.</remarks>
        /// <param name="parameters">The parameters containing user identifiers and query options for retrieving work summaries.</param>
        /// <returns>An <see cref="IActionResult"/> containing the work summary data and a cache token. The token is <see
        /// cref="Guid.Empty"/> if no summaries are found.</returns>

        [HttpPost]
        public async Task<IActionResult> GetWorkSummary([FromBody] UserWorkSummaryQueriesParameters parameters)
        {
            var summary = await _summaryService.GetWorkSummariesForUsers(parameters);
            var token = summary?.Count > 0
                ? await _summaryCached.CacheUserWork(summary)
                : Guid.Empty;

            return Ok(new { summary, token });
        }

        /// <summary>
        /// Retrieves a summary of team work activities based on the specified query parameters.
        /// </summary>
        /// <param name="parameters">The parameters used to filter and query the team work summary data.</param>
        /// <returns>An <see cref="IActionResult"/> containing the team work summary and a token. The token is a <see
        /// cref="Guid"/> that is non-empty if the summary contains data; otherwise, it is <see cref="Guid.Empty"/>.</returns>

        [HttpPost("Team")]
        public async Task<IActionResult> GetTeamWorksSummary([FromBody] TeamsWorkSummaryQueriesParameters parameters)
        {
            var summary = await _summaryService.GetTeamsWorkSummary(parameters);
            var token = summary?.Count > 0
                ? await _summaryCached.CacheUserWork(summary)
                : Guid.Empty;
            return Ok(new { summary, token });
        }

        /// <summary>
        /// Retrieves a summary of work for specified projects based on the provided query parameters.
        /// </summary>
        /// <param name="parameters">The parameters used to filter and query the projects' work summary. Cannot be null.</param>
        /// <returns>An <see cref="IActionResult"/> containing the projects' work summary and a token. The token is a <see
        /// cref="Guid"/> that is non-empty if the summary contains data; otherwise, it is <see cref="Guid.Empty"/>.</returns>
        [HttpPost("Project")]
        public async Task<IActionResult> GetProjecstWorkSummary([FromBody] ProjectsWorkSummaryQueriesParameters parameters)
        {
            var summary = await _summaryService.GetProjectsWorkSummary(parameters);
            var token = summary?.Count > 0
                ? await _summaryCached.CacheUserWork(summary)
                : Guid.Empty;
            return Ok(new { summary, token });
        }

        /// <summary>
        /// Exports a work summary as a CSV file.
        /// </summary>
        /// <remarks>The CSV file is generated based on the specified <paramref name="token"/> and is
        /// returned with a MIME type of "text/csv". The file name includes a timestamp to ensure uniqueness.</remarks>
        /// <param name="token">The unique identifier for the work summary to be exported.</param>
        /// <returns>An <see cref="IActionResult"/> containing the CSV file of the work summary.</returns>
        [HttpGet("export/{token}")]
        public async Task<IActionResult> ExportCsv(Guid token)
        {
            var csvBytes = await _summaryCached.ExportCsv(token);

            return File(csvBytes, "text/csv", $"work-summary-{DateTime.Now:yyyyMMddHHmmss}.csv");
        }


    }
}
