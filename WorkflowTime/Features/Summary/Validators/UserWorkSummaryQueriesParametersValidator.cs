using FluentValidation;
using WorkflowTime.Features.Summary.Queries;

namespace WorkflowTime.Features.Summary.Validators
{
    public class UserWorkSummaryQueriesParametersValidator : AbstractValidator<UserWorkSummaryQueriesParameters>
    {
        public UserWorkSummaryQueriesParametersValidator()
        {
            RuleFor(x => x.PeriodStart)
                .NotEmpty()
                .WithMessage("Period start date is required.")
                .LessThanOrEqualTo(x => x.PeriodEnd)
                .WithMessage("Period start date must be less than or equal to period end date.");
            RuleFor(x => x.PeriodEnd)
                .NotEmpty()
                .WithMessage("Period end date is required.");
        }
    }
}
