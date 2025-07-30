using FluentValidation;
using WorkflowTime.Features.TeamManagement.Dtos;

namespace WorkflowTime.Features.TeamManagement.Validators
{
    public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
    {
        public CreateTeamDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Team name is required.")
                .MaximumLength(100).WithMessage("Team name must not exceed 100 characters.");
        }
    }
}
