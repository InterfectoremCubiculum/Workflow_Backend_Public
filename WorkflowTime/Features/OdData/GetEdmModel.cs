using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using WorkflowTime.Features.WorkLog.Models;

namespace WorkflowTime.Features.OdData
{
    public static class EdmModelProvider
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TimeSegment>("Worklog");
            return builder.GetEdmModel();
        }
    }
}
