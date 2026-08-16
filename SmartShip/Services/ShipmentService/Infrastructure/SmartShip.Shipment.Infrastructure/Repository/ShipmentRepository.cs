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

    public async Task<Shipments?> GetByIdAndCustomerAsync(
    int shipmentId,
    int customerId)
    {
        var database = _context.Database.GetDbConnection().Database;
        var server = _context.Database.GetDbConnection().DataSource;

        Console.WriteLine($"DATABASE: {database}");
        Console.WriteLine($"SERVER: {server}");
        Console.WriteLine($"ShipmentId: {shipmentId}");
        Console.WriteLine($"CustomerId: {customerId}");

        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            Console.WriteLine("SHIPMENT NOT FOUND BY ID");
            return null;
        }

        Console.WriteLine($"DB Shipment CustomerId: {shipment.CustomerId}");

        if (shipment.CustomerId != customerId)
        {
            Console.WriteLine("CUSTOMER ID DOES NOT MATCH");
            return null;
        }

        return shipment;
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