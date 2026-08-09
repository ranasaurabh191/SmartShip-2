namespace SmartShip.Shipment.Domain.Entities;

public class ShipmentRoute
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public int? HubId { get; set; }
    public string HubName { get; set; } = string.Empty;
    public string HubCity { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int SequenceOrder { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? ReachedAt { get; set; }

    public Shipments Shipment { get; set; } = null!;
}
