using FluentValidation;
using WorkflowTime.Features.WorkLog.Queries;

namespace WorkflowTime.Features.WorkLog.Validators
{
    public class UserWorkLogQueryParametersValidator : AbstractValidator<UserWorkLogQueryParameters>
    {
        public UserWorkLogQueryParametersValidator()
        {
            RuleFor(x => x.UserId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("UserId, if provided, must be a valid non-empty GUID.");
            RuleFor(x => x.StartTime)
                .NotEmpty()
                .WithMessage("StartTime is required.")
                .LessThanOrEqualTo(x => x.EndTime)
                .WithMessage("StartTime must be less than or equal to EndTime.");
            RuleFor(x => x.EndTime)
                .NotEmpty()
                .WithMessage("EndTime is required.")
                .GreaterThanOrEqualTo(x => x.StartTime)
                .WithMessage("EndTime must be greater than or equal to StartTime.");
        }
    }
}
