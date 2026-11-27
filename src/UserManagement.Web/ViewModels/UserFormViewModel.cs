using System.ComponentModel.DataAnnotations;
using UserManagement.Web.Models;

namespace UserManagement.Web.ViewModels;

/// <summary>Used by the admin Create and Edit screens.</summary>
public class UserFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    public bool IsEdit => Id.HasValue;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "{0} must be between {2} and {1} characters.")]
    [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = ValidationPatterns.PersonNameMessage)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "{0} must be between {2} and {1} characters.")]
    [RegularExpression(ValidationPatterns.PersonName, ErrorMessage = ValidationPatterns.PersonNameMessage)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailMessage)]
    [DataType(DataType.EmailAddress)]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a role.")]
    public UserRole? Role { get; set; }

    /// <summary>Required on create; optional on edit (leave blank to keep the current password).</summary>
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} must be between {2} and {1} characters.")]
    [RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordMessage)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string? ConfirmPassword { get; set; }

    // ---- Address (optional; shown on the Edit page only) -------------------------------------------
    // Leave every address field blank to have no address. If any field is filled, the required ones must all be filled.

    [StringLength(200, ErrorMessage = "{0} can be at most {1} characters.")]
    [Display(Name = "Address 1")]
    public string? Address1 { get; set; }

    [StringLength(200, ErrorMessage = "{0} can be at most {1} characters.")]
    [Display(Name = "Address 2")]
    public string? Address2 { get; set; }

    [StringLength(100, ErrorMessage = "{0} can be at most {1} characters.")]
    public string? City { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    [RegularExpression(ValidationPatterns.Pincode, ErrorMessage = ValidationPatterns.PincodeMessage)]
    public string? Pincode { get; set; }

    /// <summary>True when at least one address field has a value.</summary>
    public bool HasAddress => new[] { Address1, Address2, City, State, Country, Pincode }.Any(v => !string.IsNullOrWhiteSpace(v));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEdit && string.IsNullOrEmpty(Password))
            yield return new ValidationResult("Password is required.", [nameof(Password)]);

        if (!HasAddress)
            yield break;

        if (string.IsNullOrWhiteSpace(Address1))
            yield return new ValidationResult("Address 1 is required when an address is entered.", [nameof(Address1)]);
        if (string.IsNullOrWhiteSpace(City))
            yield return new ValidationResult("City is required when an address is entered.", [nameof(City)]);
        if (string.IsNullOrWhiteSpace(Pincode))
            yield return new ValidationResult("Pincode is required when an address is entered.", [nameof(Pincode)]);

        if (string.IsNullOrWhiteSpace(State))
            yield return new ValidationResult("Please select a state.", [nameof(State)]);
        else if (!AddressOptions.IndianStates.Contains(State))
            yield return new ValidationResult("Please select a valid state.", [nameof(State)]);

        if (string.IsNullOrWhiteSpace(Country))
            yield return new ValidationResult("Please select a country.", [nameof(Country)]);
        else if (!AddressOptions.Countries.Contains(Country))
            yield return new ValidationResult("Please select a valid country.", [nameof(Country)]);
    }
}
