using FluentValidation;
using WorkflowTime.Features.ProjectManagement.Queries;

namespace WorkflowTime.Features.ProjectManagement.Validators
{
    public class ProjectSearchQueryParametersValidator : AbstractValidator<ProjectSearchQueryParameters>
    {
        public ProjectSearchQueryParametersValidator()
        {
            RuleFor(x => x.SearchingPhrase)
                .NotEmpty()
                .MinimumLength(3)
                .WithMessage("Search phrase must be at least 3 characters long.");

            RuleFor(x => x.ResponseLimit)
                .GreaterThan(0)
                .LessThanOrEqualTo(100)
                .WithMessage("Response limit must be between 1 and 100.");
        }
    }
}
