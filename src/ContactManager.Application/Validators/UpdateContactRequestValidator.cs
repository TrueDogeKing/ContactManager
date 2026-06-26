using ContactManager.Application.DTOs.Contacts;
using FluentValidation;

namespace ContactManager.Application.Validators;

public class UpdateContactRequestValidator : AbstractValidator<UpdateContactRequestDto>
{
    public UpdateContactRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithMessage("Invalid email address.")
            .MaximumLength(256)
            .WithMessage("Email address must not exceed 256 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .MaximumLength(32)
            .WithMessage("Phone number must not exceed 32 characters.");

        RuleFor(x => x.BirthDate)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Birth date must be in the past.");

        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Category is required.");

        RuleFor(x => x.CustomSubcategory)
            .MaximumLength(100)
            .WithMessage("Custom subcategory must not exceed 100 characters.");
    }
}
