namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a payment transaction for a shipment fails or is rejected by the payment gateway.
/// Triggers compensating commands (e.g. UpdateShipmentStatusToPaymentFailedCommand) in ShipmentService.
/// </summary>
public class PaymentFailedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the target shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number of the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID owning the shipment.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the descriptive error or failure reason returned by Razorpay or system validation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the payment transaction failed.
    /// </summary>
    public DateTime FailedAt { get; set; }
}