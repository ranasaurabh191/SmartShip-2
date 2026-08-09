using SmartShip.Shipment.Domain.Entities;

namespace SmartShip.Shipment.Core.Interfaces.Repositories;

public interface IShipmentRepository
{
    Task<Shipments?> GetByIdWithDetailsAsync(int id);
    Task<Shipments?> GetByIdAsync(int id);
    Task<Shipments?> GetByIdAndCustomerAsync(int shipmentId, int customerId);
    Task AddAsync(Shipments shipment);
    void Update(Shipments shipment);
    Task<IEnumerable<Shipments>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Shipments>> GetAllAsync();
    Task<Shipments?> GetByTrackingNumberAsync(string trackingNumber);
}