// WorkflowTime.Extensions/SwaggerExtensions.cs

using Microsoft.OpenApi.Models;
using WorkflowTime.Features.OdData;

namespace WorkflowTime.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddApplicationSwaggerServices(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.OperationFilter<ODataOperationFilter>();
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri("/api/Auth/login", UriKind.Relative),
                            TokenUrl = new Uri("/api/Auth/login", UriKind.Relative),
                        }
                    }
                });
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            return services;
        }
    }
}