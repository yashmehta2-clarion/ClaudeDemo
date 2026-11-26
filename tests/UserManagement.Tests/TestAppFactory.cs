using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Web.Data;

namespace UserManagement.Tests;

/// <summary>Runs the real app with an in-memory database instead of SQL Server.</summary>
public class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused");
        builder.UseSetting("SeedAdmin:Email", "admin@example.com");
        builder.UseSetting("SeedAdmin:Password", "Admin@12345");

        builder.ConfigureServices(services =>
        {
            foreach (var d in services.Where(d =>
                         d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                         d.ServiceType == typeof(DbContextOptions) ||
                         d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>)).ToList())
                services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}
