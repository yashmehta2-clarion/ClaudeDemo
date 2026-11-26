using System.ComponentModel.DataAnnotations;

namespace UserManagement.Web.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Email address; also used as the login user name. Unique.</summary>
    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
