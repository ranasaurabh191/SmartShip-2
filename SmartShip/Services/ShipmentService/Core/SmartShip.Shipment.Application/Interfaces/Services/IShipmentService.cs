using SmartShip.Shipment.Core.DTOs;
using SmartShip.Shipment.Domain.Enums;

namespace SmartShip.Shipment.Core.Interfaces.Services
{
    public interface IShipmentService
    {
        Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId);
        Task<ShipmentResponse> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, UpdateStatusRequest request);
        Task SchedulePickupAsync(int id, int customerId, SchedulePickupRequest request);
        Task CancelByCustomerAsync(int shipmentId, int customerId, string reason);
        Task<ShipmentResponse?> GetByTrackingNumberAsync(string trackingNumber);
        Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type);

    }
}
