

using SmartShip.Shipment.Core.Interfaces.Repositories;
using SmartShip.Shipment.Domain.Entities;
using SmartShip.Shipment.Infrastructure.Context;

namespace SmartShip.Shipment.Infrastructure.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly ShipmentDbContext _context;

    public PackageRepository(ShipmentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Package package)
    {
        await _context.Packages.AddAsync(package);
    }
}