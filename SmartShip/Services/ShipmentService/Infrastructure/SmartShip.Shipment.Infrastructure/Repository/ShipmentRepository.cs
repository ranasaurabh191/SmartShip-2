using Microsoft.EntityFrameworkCore;
using SmartShip.Shipment.Core.Interfaces.Repositories;
using SmartShip.Shipment.Domain.Entities;
using SmartShip.Shipment.Infrastructure.Context;


namespace SmartShip.Shipment.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ShipmentDbContext _context;

    public ShipmentRepository(ShipmentDbContext context)
    {
        _context = context;
    }


    public async Task<IEnumerable<Shipments>> GetByCustomerIdAsync(int customerId)
    => await _context.Shipments
        .Where(s => s.CustomerId == customerId)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();

    public async Task<IEnumerable<Shipments>> GetAllAsync()
    => await _context.Shipments
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();

 
    public async Task<Shipments?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Shipments
            .Include(s => s.SenderAddress)
            .Include(s => s.ReceiverAddress)
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
    public async Task<Shipments?> GetByTrackingNumberAsync(string trackingNumber)
    {
        return await _context.Shipments
            .Include(s => s.SenderAddress)
            .Include(s => s.ReceiverAddress)
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);
    }
    public async Task<Shipments?> GetByIdAsync(int id)
    {
        return await _context.Shipments
            .Include(s => s.SenderAddress)
            .Include(s => s.ReceiverAddress)
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Shipments?> GetByIdAndCustomerAsync(int shipmentId, int customerId)
    {
        return await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId && s.CustomerId == customerId);
    }

    public async Task AddAsync(Shipments shipment)
    {
        await _context.Shipments.AddAsync(shipment);
    }

    public void Update(Shipments shipment)
    {
        _context.Shipments.Update(shipment);
    }
}