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
        public SummaryController(ISummaryService summariseService)
        {
            _summaryService = summariseService;
        }

        [HttpPost]
        public async Task<IActionResult> GetWorkSummary([FromBody] UserWorkSummaryQueriesParameters parameters)
        {
            if (parameters.IsDayByDay)
                return Ok(await _summaryService.GetWorkSummariesDayByDayForUsers(parameters));
            else
                return Ok(await _summaryService.GetWorkSummariesForUsers(parameters));
        }

        [HttpPost("Team")]
        public async Task<IActionResult> GetTeamWorksSummary([FromBody] TeamsWorkSummaryQueriesParameters parameters)
        {
            if (parameters.IsDayByDay)
                return Ok(await _summaryService.GetTeamsWorkSummarDayByDay(parameters));
            else
                return Ok(await _summaryService.GetTeamsWorkSummary(parameters));
        }

        [HttpPost("Project")]
        public async Task<IActionResult> GetProjecstWorkSummary([FromBody] ProjectsWorkSummaryQueriesParameters parameters)
        {
            if (parameters.IsDayByDay)
                return Ok(await _summaryService.GetProjectsWorkSummaryDayByDay(parameters));
            else
                return Ok(await _summaryService.GetProjectsWorkSummary(parameters));
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportCsv([FromBody] UserWorkSummaryQueriesParameters parameters)
        {
            var csvFile = await _summaryService.ExportToCSV(parameters);
            return csvFile;
        }



    }
}
