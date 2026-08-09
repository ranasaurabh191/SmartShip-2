using Microsoft.EntityFrameworkCore;
using SmartShip.Shipment.Domain.Entities;

namespace SmartShip.Shipment.Infrastructure.Context;

public class ShipmentDbContext : DbContext
{
    public ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : base(options) { }

    public DbSet<Shipments> Shipments => Set<Shipments>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Package> Packages => Set<Package>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipments>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TrackingNumber).IsUnique();
            e.Property(s => s.ShippingRate).HasPrecision(18, 2);
            e.Property(s => s.ShipmentType).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(s => s.SenderAddress).WithMany().HasForeignKey(s => s.SenderAddressId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ReceiverAddress).WithMany().HasForeignKey(s => s.ReceiverAddressId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Package).WithMany().HasForeignKey(s => s.PackageId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Package>(e =>
        {
            e.Property(p => p.DeclaredValue)
             .HasColumnType("decimal(18,2)");
        });
  
    }
}
