using FluentValidation;
using WorkflowTime.Features.ProjectManagement.Dtos;

namespace WorkflowTime.Features.ProjectManagement.Validators
{
    public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
    {
        public CreateProjectDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Project name is required.");
        }
    }
}
