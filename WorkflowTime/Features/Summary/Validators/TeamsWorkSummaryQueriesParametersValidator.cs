using FluentValidation;
using WorkflowTime.Features.Summary.Queries;

namespace WorkflowTime.Features.Summary.Validators
{
    public class TeamsWorkSummaryQueriesParametersValidator : AbstractValidator<TeamsWorkSummaryQueriesParameters>
    {
        public TeamsWorkSummaryQueriesParametersValidator()
        {
            RuleFor(x => x.PeriodStart)
                .NotEmpty()
                .WithMessage("Period start date is required.")
                .LessThanOrEqualTo(x => x.PeriodEnd)
                .WithMessage("Period start date must be less than or equal to period end date.");
            RuleFor(x => x.PeriodEnd)
                .NotEmpty()
                .WithMessage("Period end date is required.");
            RuleFor(x => x.TeamIds)
                .NotEmpty()
                .WithMessage("Team ID is required.");
        }
    }
}
