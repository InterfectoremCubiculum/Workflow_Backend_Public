using FluentValidation;
using WorkflowTime.Features.WorkLog.Queries;

namespace WorkflowTime.Features.WorkLog.Validators
{
    public class GroupTimelineWorklogQueryParametersValidator : AbstractValidator<GroupTimelineWorklogQueryParameters>
    {
        public GroupTimelineWorklogQueryParametersValidator()
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
            RuleFor(x => x.GroupId)
                .NotEmpty()
                .WithMessage("GroupId is required.");
        }
    }
}
