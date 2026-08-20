namespace SmartShip.Payment.Application.DTOs;

/// <summary>
/// Data Transfer Object carrying shipment details required by PaymentService to calculate billing and verify ownership.
/// </summary>
public class ShipmentDTOs
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal ShippingRate { get; set; }
    public string ShipmentType { get; set; } = "Domestic";
    public bool IsFragile { get; set; }
    public double SenderLat { get; set; }
    public double SenderLng { get; set; }
    public double ReceiverLat { get; set; }
    public double ReceiverLng { get; set; }
}