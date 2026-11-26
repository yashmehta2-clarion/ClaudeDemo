using System.ComponentModel.DataAnnotations;
using UserManagement.Web.ViewModels;

namespace UserManagement.Tests;

/// <summary>
/// Unit tests for the strict email rule applied to the Register and admin Create/Edit forms.
/// Validates through <see cref="Validator.TryValidateProperty"/> so the attributes run exactly as MVC applies them.
/// </summary>
public class EmailValidationTests
{
    private static bool IsValid(object model, string propertyName, string value) =>
        Validator.TryValidateProperty(value, new ValidationContext(model) { MemberName = propertyName }, new List<ValidationResult>());

    [Theory]
    [InlineData("abc@aaa")]          // no top-level domain (reported defect)
    [InlineData("abc@aaa.")]         // empty top-level domain
    [InlineData("abc@.com")]         // empty domain label
    [InlineData("abc@aaa.c")]        // top-level domain shorter than 2 letters
    [InlineData("a..b@aaa.com")]     // consecutive dots in local part
    [InlineData("abc@-aaa.com")]     // domain label starting with a hyphen
    [InlineData("abc aaa@aaa.com")]  // space
    [InlineData("abc@aaa..com")]     // consecutive dots in domain
    public void StrictEmail_rejects_invalid_addresses(string email)
    {
        Assert.False(IsValid(new RegisterViewModel(), nameof(RegisterViewModel.Email), email));
        Assert.False(IsValid(new UserFormViewModel(), nameof(UserFormViewModel.Email), email));
    }

    [Theory]
    [InlineData("abc@aaa.com")]
    [InlineData("first.last+tag@mail.example.co.uk")]
    [InlineData("a_b-c@sub-domain.io")]
    [InlineData("ABC@AAA.COM")]
    public void StrictEmail_accepts_valid_addresses(string email)
    {
        Assert.True(IsValid(new RegisterViewModel(), nameof(RegisterViewModel.Email), email));
        Assert.True(IsValid(new UserFormViewModel(), nameof(UserFormViewModel.Email), email));
    }
}
