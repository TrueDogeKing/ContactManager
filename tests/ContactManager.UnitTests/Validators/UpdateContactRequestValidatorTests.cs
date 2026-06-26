using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Validators;
using FluentValidation.TestHelper;

namespace ContactManager.UnitTests.Validators;

public class UpdateContactRequestValidatorTests
{
    private readonly UpdateContactRequestValidator _validator = new();

    private static UpdateContactRequestDto Valid() =>
        new(
            "Jan",
            "Kowalski",
            "jan@example.com",
            "+48123456789",
            new DateOnly(1990, 1, 1),
            1,
            null,
            null,
            RowVersion: 1
        );

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FirstName_Empty_Fails()
    {
        var result = _validator.TestValidate(Valid() with { FirstName = "" });

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Email_InvalidOrEmpty_Fails(string email)
    {
        var result = _validator.TestValidate(Valid() with { Email = email });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void BirthDate_Future_Fails()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var result = _validator.TestValidate(Valid() with { BirthDate = future });

        result.ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    [Fact]
    public void CategoryId_NotPositive_Fails()
    {
        var result = _validator.TestValidate(Valid() with { CategoryId = 0 });

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void CustomSubcategory_TooLong_Fails()
    {
        var result = _validator.TestValidate(
            Valid() with
            {
                CustomSubcategory = new string('a', 101),
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.CustomSubcategory);
    }
}
