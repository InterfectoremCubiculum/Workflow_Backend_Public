using FluentValidation;
using WorkflowTime.Features.UserManagement.Queries;

namespace WorkflowTime.Features.UserManagement.Validators
{
    public class UserSearchQueryParametersValidator : AbstractValidator<UserSearchQueryParameters>
    {
        public UserSearchQueryParametersValidator()
        {
            RuleFor(x => x.SearchingPhrase)
                .MinimumLength(3)
                .WithMessage("Search phrase must be at least 3 characters long.");

            RuleFor(x => x.ResponseLimit)
                .GreaterThan(0)
                .WithMessage("Response limit must be greater than 0.")
                .LessThanOrEqualTo(100)
                .WithMessage("Response limit must not exceed 100.");
        }
    }
}
