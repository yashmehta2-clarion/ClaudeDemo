using UserManagement.Web.Models;

namespace UserManagement.Web.ViewModels;

public class UserListQuery
{
    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public string SortBy { get; set; } = UserSortColumns.LastName;
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public bool Descending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}

public static class UserSortColumns
{
    public const string FirstName = "firstname";
    public const string LastName = "lastname";
    public const string Email = "email";
    public const string Role = "role";
    public const string Created = "created";

    public static readonly string[] All = [FirstName, LastName, Email, Role, Created];
}

public class UserListViewModel
{
    public required UserListQuery Query { get; init; }
    public required IReadOnlyList<AppUser> Users { get; init; }
    public required int TotalCount { get; init; }
    public required bool IsAdmin { get; init; }
    public int? CurrentUserId { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Query.PageSize));
    public static readonly int[] PageSizes = [5, 10, 25, 50];
}
