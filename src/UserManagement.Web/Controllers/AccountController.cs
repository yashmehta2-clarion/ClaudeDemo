using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserManagement.Web.Models;
using UserManagement.Web.Services;
using UserManagement.Web.ViewModels;

namespace UserManagement.Web.Controllers;

[AllowAnonymous]
public class AccountController(IUserService users, IConfiguration config, ILogger<AccountController> logger) : Controller
{
    private bool AllowAdminSelfRegistration => config.GetValue("Registration:AllowAdminRole", true);

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Users");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var user = await users.ValidateCredentialsAsync(model.Username, model.Password, HttpContext.RequestAborted);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthCookieHelper.CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        logger.LogInformation("User {Email} signed in.", user.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Users");
    }

    [HttpGet]
    public IActionResult Register()
    {
        PopulateRoles();
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == UserRole.Admin && !AllowAdminSelfRegistration)
            ModelState.AddModelError(nameof(model.Role), "Administrator accounts can only be created by an administrator.");

        if (ModelState.IsValid && await users.EmailExistsAsync(model.Email, ct: HttpContext.RequestAborted))
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");

        if (!ModelState.IsValid)
        {
            PopulateRoles();
            return View(model);
        }

        await users.CreateAsync(model.FirstName, model.LastName, model.Email, model.Role!.Value, model.Password, HttpContext.RequestAborted);
        TempData["Success"] = "Registration successful. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private void PopulateRoles()
    {
        var roles = AllowAdminSelfRegistration ? [UserRole.User, UserRole.Admin] : new[] { UserRole.User };
        ViewBag.Roles = roles.Select(r => new SelectListItem(r.ToString(), r.ToString()));
    }
}
