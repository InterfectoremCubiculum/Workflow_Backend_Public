using FluentValidation;
using WorkflowTime.Features.DayOffs.Dtos;

namespace WorkflowTime.Features.DayOffs.Validations
{
    public class CreateDayOffRequestDtoValidator : AbstractValidator<CreateDayOffRequestDto>
    {
        public CreateDayOffRequestDtoValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required.")
                .LessThanOrEqualTo(x => x.EndDate).WithMessage("Start date must be before or equal to end date.");
            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required.")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be after or equal to start date.");
            RuleFor(x => x.RequestStatus)
                .IsInEnum().WithMessage("Invalid request status.");
        }
    }
}
