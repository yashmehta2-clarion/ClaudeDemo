using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagement.Web.Models;

namespace UserManagement.Web.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations (when enabled) and creates the default admin account
    /// if the database has no administrator yet.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        if (db.Database.IsRelational())
        {
            if (config.GetValue("Database:ApplyMigrationsOnStartup", true))
                await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var email = config["SeedAdmin:Email"];
        var password = config["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No admin user exists and SeedAdmin settings are empty; skipping admin seed.");
            return;
        }

        var admin = new AppUser
        {
            FirstName = config["SeedAdmin:FirstName"] ?? "System",
            LastName = config["SeedAdmin:LastName"] ?? "Administrator",
            Email = email.Trim().ToLowerInvariant(),
            Role = UserRole.Admin
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default admin account {Email}.", admin.Email);
    }
}
