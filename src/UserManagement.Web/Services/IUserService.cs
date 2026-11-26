using UserManagement.Web.Models;
using UserManagement.Web.ViewModels;

namespace UserManagement.Web.Services;

public interface IUserService
{
    Task<AppUser?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default);

    /// <summary>Returns the user when the email/password pair is valid; otherwise null.</summary>
    Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);

    Task<AppUser> CreateAsync(string firstName, string lastName, string email, UserRole role, string password, CancellationToken ct = default);
    Task<AppUser?> UpdateAsync(int id, string firstName, string lastName, string email, UserRole role, string? newPassword, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Filtered, sorted and paged list. When <paramref name="visibleRole"/> is set, only users of that role are returned.</summary>
    Task<(IReadOnlyList<AppUser> Items, int TotalCount)> QueryAsync(UserListQuery query, UserRole? visibleRole, CancellationToken ct = default);
}
