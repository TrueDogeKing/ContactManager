using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Validators;
using FluentValidation.TestHelper;

namespace ContactManager.UnitTests.Validators;

public class ChangeContactPasswordRequestValidatorTests
{
    private readonly ChangeContactPasswordRequestValidator _validator = new();

    [Fact]
    public void ComplexPassword_PassesValidation()
    {
        var result = _validator.TestValidate(
            new ChangeContactPasswordRequestDto("Password123!", RowVersion: 1)
        );

        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    [Theory]
    [InlineData("")] // empty
    [InlineData("Short1!")] // too short (7 chars)
    [InlineData("password123!")] // no uppercase letter
    [InlineData("Password1234")] // no special character
    public void WeakPassword_Fails(string password)
    {
        var result = _validator.TestValidate(
            new ChangeContactPasswordRequestDto(password, RowVersion: 1)
        );

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
