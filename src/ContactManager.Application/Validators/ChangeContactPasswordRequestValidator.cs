using ContactManager.Application.DTOs.Contacts;
using FluentValidation;

namespace ContactManager.Application.Validators;

public class ChangeContactPasswordRequestValidator : AbstractValidator<ChangeContactPasswordRequestDto>
{
    public ChangeContactPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}
