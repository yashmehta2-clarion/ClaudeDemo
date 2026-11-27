using System.ComponentModel.DataAnnotations;

namespace UserManagement.Web.Models;

/// <summary>One optional postal address per user (one-to-one with <see cref="AppUser"/>).</summary>
public class UserAddress
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Address1 { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Address2 { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Country { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string Pincode { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
