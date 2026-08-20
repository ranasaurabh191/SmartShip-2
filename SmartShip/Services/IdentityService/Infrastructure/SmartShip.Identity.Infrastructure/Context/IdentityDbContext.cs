using Microsoft.EntityFrameworkCore;
using SmartShip.Identity.Domain.Entities;

namespace SmartShip.Identity.Infrastructure.Data;

/// Entity Framework Core database context for managing identity and user account persistence.
/// Configures table schemas, entity constraints, unique indexes, and default column values.
public class IdentityDbContext : DbContext
{
    /// <param name="options">The DbContext options configured via Dependency Injection.</param>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();

    /// Configures entity mappings, primary keys, unique constraints, and property defaults during model initialization.
    /// <param name="modelBuilder">The model builder instance used to configure database schema rules.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("CUSTOMER");
        });
    }
}
