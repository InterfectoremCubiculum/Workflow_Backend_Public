using Hangfire;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OData;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Polly;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using WorkflowTime.Configuration;
using WorkflowTime.Database;
using WorkflowTime.Extensions;
using WorkflowTime.Features.AdminPanel;
using WorkflowTime.Features.AdminPanel.Services;
using WorkflowTime.Features.OdData;
using WorkflowTime.Mappers;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace WorkflowTime
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            // Add services to the container.
            builder.Services.AddControllers(options =>
             {
                 var policy = new AuthorizationPolicyBuilder()
                     .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                     .RequireAuthenticatedUser()
                     .Build();
                 options.Filters.Add(new AuthorizeFilter(policy));
             })
                 .AddJsonOptions(options =>
                 {
                     options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                 })
                 .AddOData(opt =>
                     opt.AddRouteComponents("odata", EdmModelProvider.GetEdmModel())
                     .Select()
                     .Filter()
                     .OrderBy()
                     .Expand());

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Resilience configuration
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("PerUserPolicy", context =>
                {
                    var userName = context.User.Identity?.IsAuthenticated == true
                        ? context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                        : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                    return RateLimitPartition.GetFixedWindowLimiter(userName, key => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
                });
            });

            builder.Services.AddRetryResilience();

            // Swagger configuration
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddApplicationSwaggerServices();

            // Services
            builder.Services.AddApplicationOptionsServices(configuration);
            builder.Services.AddApplicationServices();

            // Add database context
            var connectionString = builder.Configuration.GetConnectionString("workflowTimeConnectionString") ?? throw new InvalidOperationException("Connection string 'workflowTimeConnectionString' not found.");
            builder.Services.AddDbContext<WorkflowTimeDbContext>(options => options.UseSqlServer(connectionString));

            var azureAdOptions = configuration.GetSection("AzureAd").Get<AzureAdOptions>();
            var frontendAppUrl = configuration.GetSection("FrontendAppUrl").Get<AppFrontendUrlOptions>();

            // Add Microsoft Identity Web API authentication
            builder.Services.AddWorkflowTimeAuthentication(configuration, frontendAppUrl, azureAdOptions);

            // Add Microsoft Graph client credentials
            builder.Services.AddMicrosoftGraphClientCredentials(azureAdOptions);

            // Add Microsoft Identity Web API authorization
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("MinUserAccess", policy =>
                    policy.RequireRole("User", "Admin"))
                .AddPolicy("OnlyAdministratorAccess", policy =>
                    policy.RequireRole("Admin"));

            //Auto Mapper
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

            // Custom validation configuration
            builder.Services.AddCustomValidation();

            // Problem details configuration
            builder.Services.AddCustomProblemDetails();

            // Hangfire configuration
            builder.Services.AddHangfire(x => x.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
            builder.Services.AddHangfireServer();

            // MediatR configuration
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SettingUpdatedEvent>());

            // SignarlR configuration
            builder.Services.AddSignalR();

            // FusionCache configuration
            builder.Services.AddFusionCache();

            // CORS configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", builder =>
                {
                    builder
                    .WithOrigins("http://localhost:3000", "https://localhost:7241", " https://living-joey-clear.ngrok-free.app", "http://localhost:5100", "https://blue-flower-0651b0a03.2.azurestaticapps.net")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });

            var microsoftAppOptions = configuration.GetSection("MicrosoftApp").Get<MicrosoftAppOptions>();
            builder.Services.AddSingleton<BotFrameworkAuthentication>(sp =>
            {
                var authConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["MicrosoftAppId"] = microsoftAppOptions.AppId,
                        ["MicrosoftAppPassword"] = microsoftAppOptions.AppPassword,
                        ["MicrosoftAppTenantId"] = microsoftAppOptions.AppTenantId,
                        ["MicrosoftAppType"] = microsoftAppOptions.AppType ?? "SingleTenant"
                    })
                    .Build();

                return new ConfigurationBotFrameworkAuthentication(authConfig);
            });

            var app = builder.Build();
            app.Use(async (context, next) =>
            
            {
                Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
                await next();
            });
            if (app.Environment.IsDevelopment())
            {

                IdentityModelEventSource.ShowPII = true;
                IdentityModelEventSource.LogCompleteSecurityArtifact = true;

                app.UseHangfireDashboard("/hangfire");

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WorkflowTime Api V1");
                    c.OAuthUsePkce();
                    c.OAuth2RedirectUrl("/swagger/oauth2-redirect.html");
                });
            }


            //Hangfire with realoaing jobs after changed by user
            using var scope = app.Services.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

            app.AddApplicationHubs();

            app.UseProblemDetails();

            //app.UseHttpsRedirection();
            app.UseCors("AllowSpecificOrigins");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapControllers()
                .RequireRateLimiting("PerUserPolicy");

            var teamsOptions = configuration.GetSection("Teams").Get<TeamsOptions>();
            HangfireExtensions.ConfigureRecurringJobs(app.Services, teamsOptions, settingsService);

            app.Run();
        }
    }
}
