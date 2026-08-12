namespace SmartShip.Shared.Events;

public class CancelShipmentCommand
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}