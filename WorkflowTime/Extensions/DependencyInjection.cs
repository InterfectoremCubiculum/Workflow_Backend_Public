using Microsoft.AspNetCore.SignalR;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using WorkflowTime.Features.AdminPanel.Services;
using WorkflowTime.Features.DayOffs.Services;
using WorkflowTime.Features.DeltaLink;
using WorkflowTime.Features.Hubs;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.NotificationsTeams;
using WorkflowTime.Features.ProjectManagement.Services;
using WorkflowTime.Features.Summary.Services;
using WorkflowTime.Features.TeamManagement.Services;
using WorkflowTime.Features.Teams.Bot;
using WorkflowTime.Features.Teams.Bot.Services;
using WorkflowTime.Features.Teams.Bot.Services.AI;
using WorkflowTime.Features.Teams.Bot.Services.Commands;
using WorkflowTime.Features.Teams.Graph;
using WorkflowTime.Features.UserManagement.Services;
using WorkflowTime.Features.WorkLog.Services;

namespace WorkflowTime.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IDayOffRequestService, DayOffRequestService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWorkLogService, WorkLogService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<DeltaLinkService>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IUserSyncService, UserSyncService>();
            services.AddScoped<ISummaryService, SummaryService>();
            services.AddScoped<ISettingsService, SettingsService>();

            services.AddScoped<WorkStateCommandService>();
            services.AddScoped<WorkStateAiService>();
            //services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
            services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();
            services.AddTransient<IBot, BotHandler>();

            services.AddScoped<ScheduledMessageService>();
            //services.AddScoped<AiWorkflowAnalyzer>();
            services.AddScoped< IWorkflowAnalyzer,OpenAiWorkflowAnalyzer>();
            services.AddScoped<ConfirmationModal>();

            services.AddScoped<WorkLogNotificationJob>();

            services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationTeamsService, NotificationTeamsService>();
            services.AddScoped<IAnomalyWorklogService, AnomalyWorklogService>();
            services.AddScoped<PresensceManager>();
            return services;
        }
    }
}
