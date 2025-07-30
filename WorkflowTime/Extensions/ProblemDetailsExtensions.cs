using WorkflowTime.Exceptions;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;

namespace WorkflowTime.Extensions
{
    public static class ProblemDetailsExtensions
    {
        public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
        {
            services.AddProblemDetails(options =>
            {
                options.Map<NotFoundException>(ex => new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });

                options.Map<BadRequestException>(ex => new ProblemDetails
                {
                    Title = "Bad request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
                options.Map<ConflictException>(ex => new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
                options.Map<ForbiddenException>(ex => new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = ex.Message,
                    Status = StatusCodes.Status403Forbidden
                });

            });

            return services;
        }
    }
}
