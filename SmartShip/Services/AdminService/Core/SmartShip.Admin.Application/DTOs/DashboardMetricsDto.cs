
namespace SmartShip.Admin.Application.DTOs
{
    public class DashboardMetricsDto
    {
        public int TotalShipments { get; set; }
        public int ActiveShipments { get; set; }
        public int DeliveredToday { get; set; }
        public int TotalCustomers { get; set; }
        public string? LastUpdatedAt { get; set; } = string.Empty;
    }
}
