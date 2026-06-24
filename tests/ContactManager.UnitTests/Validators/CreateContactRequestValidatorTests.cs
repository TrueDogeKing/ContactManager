using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Validators;
using FluentValidation.TestHelper;

namespace ContactManager.UnitTests.Validators;

public class CreateContactRequestValidatorTests
{
    private readonly CreateContactRequestValidator _validator = new();

    private static CreateContactRequestDto Valid() =>
        new("Jan", "Kowalski", "jan@example.com", "password123", "+48123456789",
            new DateOnly(1990, 1, 1), 1, null, null);

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FirstName_Empty_Fails(string firstName)
    {
        var result = _validator.TestValidate(Valid() with { FirstName = firstName });

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_TooLong_Fails()
    {
        var result = _validator.TestValidate(Valid() with { FirstName = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void LastName_Empty_Fails()
    {
        var result = _validator.TestValidate(Valid() with { LastName = "" });

        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Email_InvalidOrEmpty_Fails(string email)
    {
        var result = _validator.TestValidate(Valid() with { Email = email });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short7")]
    public void Password_TooShortOrEmpty_Fails(string password)
    {
        var result = _validator.TestValidate(Valid() with { Password = password });

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Phone_Empty_Fails()
    {
        var result = _validator.TestValidate(Valid() with { Phone = "" });

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void BirthDate_TodayOrFuture_Fails()
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
        var result = _validator.TestValidate(Valid() with { CustomSubcategory = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(x => x.CustomSubcategory);
    }
}
