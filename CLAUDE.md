# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`ClaudeDemo` is a base repo for Claude Code samples. The one sample so far is **User Management**: an ASP.NET Core MVC app on **.NET 10 (LTS)** with EF Core 10 on SQL Server, styled with AdminLTE 4.10 / Bootstrap 5.3. The solution is `UserManagement.sln` with two projects: `src/UserManagement.Web` and `tests/UserManagement.Tests`. `README.md` has the user-facing setup and feature list.

Development happens on the `FirstSample` branch (`main` only holds the initial commit). Changes go through PRs into `FirstSample`.

## Commands

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~StrictEmail"                            # one test class or method (substring match)
dotnet run --project src/UserManagement.Web --launch-profile https               # https://localhost:7180, http://localhost:5180
dotnet tool restore                                                              # installs the pinned dotnet-ef (dotnet-tools.json)
dotnet ef migrations add <Name> -o Data/Migrations --project src/UserManagement.Web
```

- There is no linter or formatter configured.
- `run-chrome.cmd` / `.ps1` / `.sh` build, run and open the site in Chrome. They are for developer machines, not CI.
- After adding a migration, regenerate `database/CreateDatabase.sql` with `dotnet ef migrations script --idempotent -o database/CreateDatabase.sql --project src/UserManagement.Web`. It is a checked-in artifact.
- The SDK is .NET 10. The target framework is set in both `.csproj` files. Package versions are pinned to `10.0.x`.

## Architecture

**Request flow:** `Controllers` → `IUserService` / `UserService` → `AppDbContext` (EF Core). Controllers hold no query logic.

**Single entity:** `AppUser` (table `Users`), with a unique index on `Email`. `Role` is the `UserRole` enum, stored as a string. `Email` doubles as the login username and is always lower-cased through `UserService.NormalizeEmail`. Use that helper for any new email lookup or write.

**Address (optional, one per user):** `UserAddress` (table `UserAddresses`) is one-to-one with `AppUser` and cascade-deleted. It is shown and saved **only on the admin Edit page**; `UserFormViewModel` carries the address fields and `HasAddress` decides whether an address exists. Blank address fields mean "no address" (`UserService.UpdateAsync` removes it when `address` is null); if any field is filled, `Validate()` requires the rest. Dropdown values live in `Models/AddressOptions.cs` (36 Indian states/UTs, countries India and Russia) and are re-checked server-side. State and Country are searchable via Tom Select, applied on top of the normal `<select>` (so posting and validation are unchanged); its CSS/JS load only on the Edit page through the layout's `Styles` section. `IValidatableObject.Validate` only runs once attribute validation passes, so some errors appear in two rounds.

**Configuration:**
- The connection string is `ConnectionStrings:DefaultConnection`. It can be overridden via user-secrets or the environment variable `ConnectionStrings__DefaultConnection`.
- `DbInitializer.InitializeAsync` runs at startup. It applies migrations unless `Database:ApplyMigrationsOnStartup` is false, and seeds an admin from `SeedAdmin:*` only if no admin exists.
- `Registration:AllowAdminRole` controls whether public registration offers the Admin role.

**Authentication:** cookie auth with the role stored as a claim. `AuthCookieHelper.ValidatePrincipalAsync` runs on every request that carries the cookie. It re-reads the user from the database, signs out deleted users, and refreshes the cookie when name, email or role changed. So role changes take effect on the next request, which is why `UserService.FindByIdAsync` is hit on each request.

**Role-based visibility (the main business rule):** `UsersController.Index` passes `visibleRole = UserRole.User` to `UserService.QueryAsync` for non-admins, and it ignores their role filter. Admin-only actions use `[Authorize(Roles = "Admin")]`. Admins cannot delete themselves or remove their own Admin role (enforced in `UsersController`).

**Validation exists in two layers.** The same rules run on the server (DataAnnotations on `ViewModels/*`) and in the browser (jQuery unobtrusive validation, generated from those attributes).
- Shared regexes and messages live in `ViewModels/ValidationPatterns.cs` (`PersonName`, `Password`, `Email`).
- They are written without `\p{..}` classes, lookbehinds, or other .NET-only syntax so the same pattern works in JavaScript. Keep any new pattern JS-compatible.
- `[EmailAddress]` is deliberately not used for registration or admin create/edit, because it accepts `abc@aaa`. `LoginViewModel` keeps the lenient `[EmailAddress]` on purpose, so older accounts saved with a weak email can still sign in.
- Required-on-create but optional-on-edit passwords are handled in `UserFormViewModel` (`IValidatableObject`).
- In `Users/Form.cshtml`, the Password input is rendered in two branches on purpose: a null `data-val-required` still renders as an empty attribute, which makes the browser demand a password on Edit.

**Front-end assets are vendored** in `src/UserManagement.Web/wwwroot/lib/` (AdminLTE, Bootstrap, Bootstrap Icons, jQuery and validation, Tom Select). There is no npm or CDN step. To upgrade, replace the files there.

**Views:** `_Layout` is the AdminLTE shell. `_AuthLayout` is the login/register card and takes `ViewData["BoxClass"]` (`login` or `register`). `Users/Index` renders the grid; filters, sorting and paging are plain GET query-string parameters bound into `UserListQuery`. `Users/Form` serves both Create and Edit.

**Dropdowns** are always have `<option value="">-- Select --</option>` as the first option. The `UserRole` enum is rendered with `Enum.GetValues<UserRole>()` and search with textboxes without case-sensitivity.
## Testing

`tests/UserManagement.Tests` has two kinds of tests:
- **Unit tests** call `Validator.TryValidateProperty` against view models (`EmailValidationTests`).
- **End-to-end tests** (`AppFlowTests`) drive the real app over HTTP through `WebApplicationFactory<Program>`. `TestAppFactory` swaps SQL Server for the EF Core **InMemory** provider and seeds `admin@example.com` / `Admin@12345`. They fetch the anti-forgery token from the form before each POST (`PostFormAsync`).

Things to know when writing tests:
- The tests share one in-memory database per fixture, so use unique emails per test.
- The InMemory provider is **case-sensitive** where SQL Server is not. Search in `UserService` lowercases explicitly so both behave the same. Keep that in mind when adding string comparisons to queries.
- The logged-in user's email also appears in the page header, so assert on `mailto:` links when checking the grid.
- Nothing here runs against a real SQL Server. Migrations and SQL Server behaviour are not covered by tests.

## Gotchas

- `Program` is declared `public partial class Program;` at the bottom of `Program.cs` so the test factory can reference it. Don't remove it.
- Seeded credentials in `appsettings.json` are demo values and must be changed for any real deployment.

##GitHub
- Any promt with keywords like "Feature", it should be a new branch from `FirstSample` and branch name as "FeatureName" and after raise a PR into `FirstSample`. The PR title should start with the feature name, e.g. `Feature: Add password reset`.