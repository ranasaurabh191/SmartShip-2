namespace SmartShip.Payment.Application.DTOs;

/// <summary>
/// Request DTO for querying payment status by Order ID, Shipment ID, or Tracking Number.
/// </summary>
public class PaymentStatusRequest
{

    public string? RazorpayOrderId { get; set; }
    public int? ShipmentId { get; set; }
    public string? TrackingNumber { get; set; }
}