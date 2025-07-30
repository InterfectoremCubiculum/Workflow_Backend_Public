using FluentValidation;
using WorkflowTime.Features.DayOffs.Queries;

namespace WorkflowTime.Features.DayOffs.Validations
{
    public class DayOffsRequestQueryParametersValidator : AbstractValidator<DayOffsRequestQueryParameters>
    {
        public DayOffsRequestQueryParametersValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("PageNumber must be greater than 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("PageSize must be greater than 0.")
                .LessThan(100)
                .WithMessage("PageSize must be less than 100.");
            RuleFor(x => x.UserId)
                .Must(id => id == null || id != Guid.Empty)
                .WithMessage("UserId must be a valid GUID or null.");
            RuleFor(x => x.SortOrder)
                .Must(order => order.Equals("asc", StringComparison.OrdinalIgnoreCase) || order.Equals("desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("SortOrder must be either 'asc' or 'desc'.");
        }
    }
}
