

using SmartShip.Shipment.Core.Interfaces.Repositories;
using SmartShip.Shipment.Domain.Entities;
using SmartShip.Shipment.Infrastructure.Context;

namespace SmartShip.Shipment.Infrastructure.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly ShipmentDbContext _context;

    public AddressRepository(ShipmentDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(params Address[] addresses)
    {
        await _context.Addresses.AddRangeAsync(addresses);
    }
}