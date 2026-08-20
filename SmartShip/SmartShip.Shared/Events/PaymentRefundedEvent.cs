namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a payment refund is processed for a cancelled or disputed shipment.
/// </summary>
public class PaymentRefundedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number of the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID receiving the refund.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the total monetary amount refunded to the customer.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the refund transaction occurred.
    /// </summary>
    public DateTime RefundedAt { get; set; } = DateTime.Now;
}