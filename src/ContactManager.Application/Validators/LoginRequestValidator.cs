using ContactManager.Application.DTOs.Auth;
using FluentValidation;

namespace ContactManager.Application.Validators;

/// <summary>Walidacja danych logowania.</summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    /// <summary>Definiuje reguły walidacji.</summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Adres e-mail jest wymagany.")
            .EmailAddress().WithMessage("Nieprawidłowy adres e-mail.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Hasło jest wymagane.");
    }
}
