using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagement.Web.Data;
using UserManagement.Web.Models;
using UserManagement.Web.ViewModels;

namespace UserManagement.Web.Services;

public class UserService(AppDbContext db, IPasswordHasher<AppUser> hasher) : IUserService
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public Task<AppUser?> FindByIdAsync(int id, CancellationToken ct = default) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default)
    {
        var normalized = NormalizeEmail(email);
        return db.Users.AnyAsync(u => u.Email == normalized && (excludeUserId == null || u.Id != excludeUserId), ct);
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var normalized = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
        if (user is null)
        {
            // Hash anyway so response time does not reveal whether the account exists.
            hasher.HashPassword(new AppUser(), password);
            return null;
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await db.SaveChangesAsync(ct);
        }
        return user;
    }

    public async Task<AppUser> CreateAsync(string firstName, string lastName, string email, UserRole role, string password, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = NormalizeEmail(email),
            Role = role
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<AppUser?> UpdateAsync(int id, string firstName, string lastName, string email, UserRole role, string? newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.Email = NormalizeEmail(email);
        user.Role = role;
        if (!string.IsNullOrEmpty(newPassword))
            user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(IReadOnlyList<AppUser> Items, int TotalCount)> QueryAsync(UserListQuery query, UserRole? visibleRole, CancellationToken ct = default)
    {
        IQueryable<AppUser> users = db.Users.AsNoTracking();

        if (visibleRole is not null)
            users = users.Where(u => u.Role == visibleRole);
        else if (query.Role is not null)
            users = users.Where(u => u.Role == query.Role);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Case-insensitive regardless of database collation.
            var term = query.Search.Trim().ToLower();
            users = users.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.Contains(term) ||
                (u.FirstName + " " + u.LastName).ToLower().Contains(term));
        }

        var desc = query.Descending;
        users = query.SortBy switch
        {
            UserSortColumns.FirstName => desc ? users.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName) : users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            UserSortColumns.Email => desc ? users.OrderByDescending(u => u.Email) : users.OrderBy(u => u.Email),
            UserSortColumns.Role => desc ? users.OrderByDescending(u => u.Role).ThenBy(u => u.LastName) : users.OrderBy(u => u.Role).ThenBy(u => u.LastName),
            UserSortColumns.Created => desc ? users.OrderByDescending(u => u.CreatedAtUtc) : users.OrderBy(u => u.CreatedAtUtc),
            _ => desc ? users.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName) : users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
        };

        var total = await users.CountAsync(ct);
        var items = await users
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
