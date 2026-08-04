using Microsoft.EntityFrameworkCore;

namespace SmartShip.Admin.Infrastructure.Context
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { } // Constructor to initialize the DbContext with options
    }
}
