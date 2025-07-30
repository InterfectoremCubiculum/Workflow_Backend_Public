using WorkflowTime.Configuration;

namespace WorkflowTime.Extensions
{
    public static class OptionsInjection
    {
        public static IServiceCollection AddApplicationOptionsServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AzureAdOptions>(configuration.GetSection("AzureAd"));
            services.Configure<AppFrontendUrlOptions>(configuration.GetSection("FrontendAppUrl"));
            services.Configure<TeamsOptions>(configuration.GetSection("Teams"));
            services.Configure<OpenAIWorkflowAnalyzerOptions>(configuration.GetSection("OpenAIWorkflowAnalyzer"));
            services.Configure<MicrosoftAppOptions>(configuration.GetSection("MicrosoftApp"));
            return services;
        }
    }
}
