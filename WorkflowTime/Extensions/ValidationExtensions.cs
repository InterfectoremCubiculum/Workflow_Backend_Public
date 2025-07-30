using FluentValidation.AspNetCore;
using FluentValidation;
using WorkflowTime.Features.ProjectManagement.Validators;
using WorkflowTime.Features.DayOffs.Validations;
using WorkflowTime.Features.UserManagement.Validators;
using WorkflowTime.Features.WorkLog.Validators;
using WorkflowTime.Features.TeamManagement.Validators;
using WorkflowTime.Features.Summary.Validators;
using WorkflowTime.Features.AdminPanel;

namespace WorkflowTime.Extensions
{
    public static class ValidationExtensions
    {
        public static IServiceCollection AddCustomValidation(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();

            //Project
            services.AddValidatorsFromAssemblyContaining<CreateProjectDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<ProjectSearchQueryParametersValidator>();

            //DayOffs
            services.AddValidatorsFromAssemblyContaining<DayOffsRequestQueryParametersValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateDayOffRequestDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateDayOffRequestDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<CalendarDayOffsRequestQueryParametersValidator>();

            //User
            services.AddValidatorsFromAssemblyContaining<UserSearchQueryParametersValidator>();

            //WorkLog
            services.AddValidatorsFromAssemblyContaining<UserWorkLogQueryParametersValidator>();
            services.AddValidatorsFromAssemblyContaining<UserTimelineWorklogQueryParametersValidator>();
            services.AddValidatorsFromAssemblyContaining<GroupTimelineWorklogQueryParametersValidator>();

            //Team Management
            services.AddValidatorsFromAssemblyContaining<CreateTeamDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<TeamSearchQueryParametersValidators>();

            //Summary
            services.AddValidatorsFromAssemblyContaining<UserWorkSummaryQueriesParametersValidator>();
            services.AddValidatorsFromAssemblyContaining<ProjectsWorkSummaryQueriesParametersValidator>();
            services.AddValidatorsFromAssemblyContaining<TeamsWorkSummaryQueriesParametersValidator>();

            //Settings
            services.AddValidatorsFromAssemblyContaining<SettingUpdatedEventValidator>();

            return services;
        }
    }
}
