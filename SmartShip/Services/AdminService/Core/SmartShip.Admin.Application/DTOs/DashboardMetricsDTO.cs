// Defines request and response DTOs used by the Identity module for authentication,
// user registration, profile management, and administrative user operations.
// API controls exactly what the client receives.

namespace SmartShip.Admin.Application.DTOs
{
    public class DashboardMetricsDTO
    {
        public int TotalShipments { get; set; }
        public int ActiveShipments { get; set; }
        public int DeliveredToday { get; set; }
        public int TotalCustomers { get; set; }
        public string? LastUpdatedAt { get; set; } = string.Empty;
    }
}
