using ContactManager.Application.DTOs.Contacts;
using FluentValidation;

namespace ContactManager.Application.Validators;

public class ChangeContactPasswordRequestValidator : AbstractValidator<ChangeContactPasswordRequestDto>
{
    public ChangeContactPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
