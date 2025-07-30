using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using WorkflowTime.Database;

namespace WorkflowTime.Features.OdData
{
    //[Authorize(Policy = "MinUserAccess")]
    [ApiController]
    [Route("odata/[controller]")]
    public class SummariseController : ControllerBase
    {

        private readonly WorkflowTimeDbContext _context;
        public SummariseController(WorkflowTimeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_context.TimeSegments.Where(ts => !ts.IsDeleted));
        }

    }
}
