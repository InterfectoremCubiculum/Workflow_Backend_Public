using FluentValidation;
using Microsoft.Graph.Models;
using System.ComponentModel.DataAnnotations;

namespace WorkflowTime.Features.UserManagement.Validators
{
    public class UserValidator : AbstractValidator<User>
    {
        public UserValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("User ID is required.");
            RuleFor(x => x.GivenName).NotEmpty().WithMessage("GivenName is required.");
            RuleFor(x => x.Surname).NotEmpty().WithMessage("Surname is required.");
            RuleFor(x => x.Mail)
                .Must(mail => string.IsNullOrWhiteSpace(mail) || new EmailAddressAttribute().IsValid(mail))
                .WithMessage("Invalid email format.");

        }
    }
}
