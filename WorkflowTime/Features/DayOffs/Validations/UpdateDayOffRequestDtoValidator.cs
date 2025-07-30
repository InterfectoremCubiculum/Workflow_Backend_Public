using FluentValidation;
using WorkflowTime.Features.DayOffs.Dtos;

namespace WorkflowTime.Features.DayOffs.Validations
{
    public class UpdateDayOffRequestDtoValidator : AbstractValidator<UpdateDayOffRequestDto>
    {
        public UpdateDayOffRequestDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.")
                .GreaterThan(0).WithMessage("Id must be greater than 0.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required.");
            //.Must(date => date >= DateOnly.FromDateTime(DateTime.Now))
            //.WithMessage("Start date must be today or in the future.");
            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required.")
                .Must((dto, endDate) => endDate >= dto.StartDate)
                .WithMessage("End date must be greater than or equal to start date.");

            RuleFor(x => x.RequestStatus)
                .IsInEnum()
                .WithMessage("Request status must be a valid enum value.");
        }
    }
}
