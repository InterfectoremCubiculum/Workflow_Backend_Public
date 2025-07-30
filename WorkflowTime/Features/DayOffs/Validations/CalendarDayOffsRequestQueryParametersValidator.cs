using FluentValidation;
using WorkflowTime.Features.DayOffs.Queries;

namespace WorkflowTime.Features.DayOffs.Validations
{
    public class CalendarDayOffsRequestQueryParametersValidator : AbstractValidator<CalendarDayOffsRequestQueryParameters>
    {
        public CalendarDayOffsRequestQueryParametersValidator()
        {
            RuleFor(x => x.From)
                .NotEmpty()
                .WithMessage("From date is required.");

            RuleFor(x => x.To)
                .NotEmpty()
                .WithMessage("To date is required.")
                .Must((dto, toDate) => toDate >= dto.From)
                .WithMessage("To date must be greater than or equal to from date.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required.")
                .Must(id => id != Guid.Empty)
                .WithMessage("User ID must be a valid GUID.");
        }
    }
}
