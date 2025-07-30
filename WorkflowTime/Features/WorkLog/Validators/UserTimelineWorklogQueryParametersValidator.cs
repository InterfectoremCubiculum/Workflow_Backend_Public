using FluentValidation;
using WorkflowTime.Queries;

namespace WorkflowTime.Features.WorkLog.Validators
{
    public class UserTimelineWorklogQueryParametersValidator : AbstractValidator<UserTimelineWorklogQueryParameters>
    {
        public UserTimelineWorklogQueryParametersValidator()
        {
            RuleFor(x => x.DateFrom)
                .NotEmpty()
                .WithMessage("DateFrom is required.")
                .LessThanOrEqualTo(x => x.DateTo)
                .WithMessage("DateFrom must be less than or equal to DateTo.");
            RuleFor(x => x.DateTo)
                .NotEmpty()
                .WithMessage("DateTo is required.")
                .GreaterThanOrEqualTo(x => x.DateFrom)
                .WithMessage("DateTo must be greater than or equal to DateFrom.");
            RuleFor(x => x.UserIds)
                .NotEmpty()
                .WithMessage("UserIds cannot be empty.");
        }
    }
}
