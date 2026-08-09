using SmartShip.Shipment.Core.DTOs;
using SmartShip.Shipment.Domain.Enums;

namespace SmartShip.ShipmentService.Core.Interfaces.Services
{
    public interface IShipmentService
    {
        Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, int customerId);
        Task<ShipmentResponse> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, UpdateStatusRequest request);
        Task SchedulePickupAsync(int id, int customerId, SchedulePickupRequest request);
        Task<decimal> CalculateRateAsync(double weightKg, ShipmentType type, double distanceKm = 0);
        Task CancelByCustomerAsync(int shipmentId, int customerId, string reason);
        Task<IEnumerable<ShipmentSummaryDto>> GetShipmentSummaryByCustomerAsync(int customerId);
        Task<ShipmentResponse?> GetByTrackingNumberAsync(string trackingNumber);
        Task<AdminSummaryDto> GetAdminSummaryAsync();
    }
}
