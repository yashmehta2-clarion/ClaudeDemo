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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEdit && string.IsNullOrEmpty(Password))
            yield return new ValidationResult("Password is required.", [nameof(Password)]);
    }
}
