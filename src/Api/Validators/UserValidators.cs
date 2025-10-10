using FluentValidation;
using Engitrack.Api.Auth;

namespace Engitrack.Api.Validators;

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required")
            .MaximumLength(120)
            .WithMessage("Full name must be 120 characters or less");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone is required")
            .MaximumLength(32)
            .WithMessage("Phone must be 32 characters or less");
    }
}