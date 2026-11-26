using System.ComponentModel.DataAnnotations;

namespace UserManagement.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    [EmailAddress(ErrorMessage = "Username must be the email address you registered with.")]
    [Display(Name = "Username (Email)")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
