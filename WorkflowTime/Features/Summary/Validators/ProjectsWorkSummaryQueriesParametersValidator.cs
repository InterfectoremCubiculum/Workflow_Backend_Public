using FluentValidation;
using WorkflowTime.Features.Summary.Queries;

namespace WorkflowTime.Features.Summary.Validators
{
    public class ProjectsWorkSummaryQueriesParametersValidator : AbstractValidator<ProjectsWorkSummaryQueriesParameters>
    {
        public ProjectsWorkSummaryQueriesParametersValidator()
        {
            RuleFor(x => x.PeriodStart)
                .NotEmpty()
                .WithMessage("Period start date is required.")
                .LessThanOrEqualTo(x => x.PeriodEnd)
                .WithMessage("Period start date must be less than or equal to period end date.");
            RuleFor(x => x.PeriodEnd)
                .NotEmpty()
                .WithMessage("Period end date is required.");
            RuleFor(x => x.ProjectIds)
                .NotEmpty()
                .WithMessage("User ID is required.");
        }
    }
}
