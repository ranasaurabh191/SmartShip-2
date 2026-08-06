using Microsoft.EntityFrameworkCore;
using SmartShip.Admin.Domain.Entities;

namespace SmartShip.Admin.Infrastructure.Context
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { } // Constructor to initialize the DbContext with options
        public DbSet<Hub> Hubs => Set<Hub>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<DashboardMetrics> DashboardMetrics => Set<DashboardMetrics>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0);

            //seed data for Hubs
            modelBuilder.Entity<Hub>().HasData(
            new Hub { Id = 101, Name = "Bangalore Hub", City = "Bengaluru", State = "Karnataka", Country = "India", ContactPhone = "9800000003", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 102, Name = "Hyderabad Hub", City = "Hyderabad", State = "Telangana", Country = "India", ContactPhone = "9800000004", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 103, Name = "Chennai Hub", City = "Chennai", State = "Tamil Nadu", Country = "India", ContactPhone = "9800000005", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 104, Name = "Kolkata Hub", City = "Kolkata", State = "West Bengal", Country = "India", ContactPhone = "9800000006", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 105, Name = "Jalandhar Hub", City = "Jalandhar", State = "Punjab", Country = "India", ContactPhone = "9800000007", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 106, Name = "Lucknow Hub", City = "Lucknow", State = "Uttar Pradesh", Country = "India", ContactPhone = "9800000008", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 107, Name = "Pune Hub", City = "Pune", State = "Maharashtra", Country = "India", ContactPhone = "9800000009", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 108, Name = "Ahmedabad Hub", City = "Ahmedabad", State = "Gujarat", Country = "India", ContactPhone = "9800000010", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 109, Name = "Jaipur Hub", City = "Jaipur", State = "Rajasthan", Country = "India", ContactPhone = "9800000011", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 110, Name = "Chandigarh Hub", City = "Chandigarh", State = "Chandigarh", Country = "India", ContactPhone = "9800000012", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 111, Name = "Indore Hub", City = "Indore", State = "Madhya Pradesh", Country = "India", ContactPhone = "9800000013", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 112, Name = "Nagpur Hub", City = "Nagpur", State = "Maharashtra", Country = "India", ContactPhone = "9800000014", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 113, Name = "Patna Hub", City = "Patna", State = "Bihar", Country = "India", ContactPhone = "9800000015", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 114, Name = "Bhopal Hub", City = "Bhopal", State = "Madhya Pradesh", Country = "India", ContactPhone = "9800000016", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 115, Name = "Kochi Hub", City = "Kochi", State = "Kerala", Country = "India", ContactPhone = "9800000017", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 116, Name = "Guwahati Hub", City = "Guwahati", State = "Assam", Country = "India", ContactPhone = "9800000018", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 117, Name = "Coimbatore Hub", City = "Coimbatore", State = "Tamil Nadu", Country = "India", ContactPhone = "9800000019", IsActive = true, CreatedAt = seedDate },
            new Hub { Id = 118, Name = "Visakhapatnam Hub", City = "Visakhapatnam", State = "Andhra Pradesh", Country = "India", ContactPhone = "9800000020", IsActive = true, CreatedAt = seedDate }
            );

            modelBuilder.Entity<DashboardMetrics>().HasData(
                new DashboardMetrics
                {
                    Id = 1,
                    TotalShipments = 0,
                    ActiveShipments = 0,
                    DeliveredToday = 0,
                    TotalCustomers = 0,
                    LastUpdatedAt = seedDate
                }
            );
        }
    }

}
