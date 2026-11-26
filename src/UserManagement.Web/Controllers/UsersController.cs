using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserManagement.Web.Models;
using UserManagement.Web.Services;
using UserManagement.Web.ViewModels;

namespace UserManagement.Web.Controllers;

[Authorize]
public class UsersController(IUserService users) : Controller
{
    private const string AdminRole = nameof(UserRole.Admin);

    private bool IsAdmin => User.IsInRole(AdminRole);

    // GET /Users?search=&role=&sortBy=&sortDir=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] UserListQuery query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = UserListViewModel.PageSizes.Contains(query.PageSize) ? query.PageSize : 10;
        if (!UserSortColumns.All.Contains(query.SortBy)) query.SortBy = UserSortColumns.LastName;
        query.SortDir = query.Descending ? "desc" : "asc";

        // Normal users may only ever see other normal users.
        UserRole? visibleRole = IsAdmin ? null : UserRole.User;
        if (!IsAdmin) query.Role = null;

        var (items, total) = await users.QueryAsync(query, visibleRole, HttpContext.RequestAborted);

        // If the requested page is past the end (e.g. after a delete), jump to the last page.
        var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)query.PageSize));
        if (query.Page > lastPage)
        {
            query.Page = lastPage;
            (items, total) = await users.QueryAsync(query, visibleRole, HttpContext.RequestAborted);
        }

        return View(new UserListViewModel
        {
            Query = query,
            Users = items,
            TotalCount = total,
            IsAdmin = IsAdmin,
            CurrentUserId = User.GetUserId()
        });
    }

    [HttpGet, Authorize(Roles = AdminRole)]
    public IActionResult Create()
    {
        PopulateRoles();
        return View("Form", new UserFormViewModel { Role = UserRole.User });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        model.Id = null;
        if (ModelState.IsValid && await users.EmailExistsAsync(model.Email, ct: HttpContext.RequestAborted))
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");

        if (!ModelState.IsValid)
        {
            PopulateRoles();
            return View("Form", model);
        }

        var user = await users.CreateAsync(model.FirstName, model.LastName, model.Email, model.Role!.Value, model.Password!, HttpContext.RequestAborted);
        TempData["Success"] = $"User '{user.FullName}' was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await users.FindByIdAsync(id, HttpContext.RequestAborted);
        if (user is null) return NotFound();

        PopulateRoles();
        return View("Form", new UserFormViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Edit(int id, UserFormViewModel model)
    {
        model.Id = id;
        var existing = await users.FindByIdAsync(id, HttpContext.RequestAborted);
        if (existing is null) return NotFound();

        // Prevent an admin from locking themselves out by removing their own admin role.
        if (id == User.GetUserId() && model.Role != UserRole.Admin)
            ModelState.AddModelError(nameof(model.Role), "You cannot remove the Admin role from your own account.");

        if (ModelState.IsValid && await users.EmailExistsAsync(model.Email, id, HttpContext.RequestAborted))
            ModelState.AddModelError(nameof(model.Email), "Another account already uses this email.");

        if (!ModelState.IsValid)
        {
            PopulateRoles();
            return View("Form", model);
        }

        var user = await users.UpdateAsync(id, model.FirstName, model.LastName, model.Email, model.Role!.Value, model.Password, HttpContext.RequestAborted);
        TempData["Success"] = $"User '{user!.FullName}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AdminRole)]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        if (id == User.GetUserId())
        {
            TempData["Error"] = "You cannot delete your own account.";
        }
        else if (await users.DeleteAsync(id, HttpContext.RequestAborted))
        {
            TempData["Success"] = "User was deleted.";
        }
        else
        {
            TempData["Error"] = "User not found.";
        }

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private void PopulateRoles() =>
        ViewBag.Roles = Enum.GetValues<UserRole>().Select(r => new SelectListItem(r.ToString(), r.ToString()));
}
