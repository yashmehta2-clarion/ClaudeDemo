using Microsoft.EntityFrameworkCore;
using UserManagement.Web.Models;

namespace UserManagement.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.Ignore(u => u.FullName);
            e.HasOne(u => u.Address).WithOne(a => a.User).HasForeignKey<UserAddress>(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAddress>().ToTable("UserAddresses");
    }
}
