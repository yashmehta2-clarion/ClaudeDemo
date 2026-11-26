using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserManagement.Web.Models;

namespace UserManagement.Web.Services;

public static class AuthCookieHelper
{
    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>
    /// Runs on every request carrying an auth cookie. Signs the user out if their account was deleted,
    /// and refreshes the cookie if their name/email/role was changed by an administrator.
    /// </summary>
    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal?.GetUserId();
        var users = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
        var user = userId is null ? null : await users.FindByIdAsync(userId.Value, context.HttpContext.RequestAborted);

        if (user is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var p = context.Principal!;
        if (p.FindFirstValue(ClaimTypes.Role) != user.Role.ToString() ||
            p.FindFirstValue(ClaimTypes.Email) != user.Email ||
            p.FindFirstValue(ClaimTypes.Name) != user.FullName)
        {
            context.ReplacePrincipal(CreatePrincipal(user));
            context.ShouldRenew = true;
        }
    }
}
