using FluentValidation;
using WorkflowTime.Features.TeamManagement.Queries;

namespace WorkflowTime.Features.TeamManagement.Validators
{
    public class TeamSearchQueryParametersValidators : AbstractValidator<TeamSearchQueryParameters>
    {
        public TeamSearchQueryParametersValidators()
        {
            RuleFor(x => x.SearchingPhrase)
                .MinimumLength(3)
                .WithMessage("Search phrase must be at least 3 characters long.");
            RuleFor(x => x.ResponseLimit)
                .GreaterThan(0).WithMessage("Response limit must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("Response limit must not exceed 100.");
        }
    }
}
