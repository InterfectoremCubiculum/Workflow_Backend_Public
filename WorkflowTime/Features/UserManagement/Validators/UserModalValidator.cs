namespace WorkflowTime.Features.UserManagement.Validators
{
    using FluentValidation;
    using System.ComponentModel.DataAnnotations;
    using WorkflowTime.Features.UserManagment.Models;

    public class UserModelValidator : AbstractValidator<UserModel>
    {
        public UserModelValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("User ID is required."); ;
            RuleFor(x => x.GivenName).NotEmpty().WithMessage("GivenName is required.");
            RuleFor(x => x.Surname).NotEmpty().WithMessage("Surname is required.");
            RuleFor(x => x.Email)
                .Must(mail => string.IsNullOrWhiteSpace(mail) || new EmailAddressAttribute().IsValid(mail))
                .WithMessage("Invalid email format.");
        }
    }

}
