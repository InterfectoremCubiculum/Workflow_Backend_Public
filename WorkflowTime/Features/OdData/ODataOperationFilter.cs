using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace WorkflowTime.Features.OdData
{
    using Microsoft.AspNetCore.OData.Query;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class ODataOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasEnableQuery = context.MethodInfo.GetCustomAttributes(typeof(EnableQueryAttribute), false).Length != 0;

            if (!hasEnableQuery)
                return;

            var odataParams = new[]
            {
            "$filter", "$orderby", "$top", "$skip", "$select", "$expand", "$count"
        };

            foreach (var param in odataParams)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = param,
                    In = ParameterLocation.Query,
                    Description = $"OData query option: {param}",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" }
                });
            }
        }
    }
}
