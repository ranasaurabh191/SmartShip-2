namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a new payment request or Razorpay order has been generated for a shipment.
/// </summary>
public class PaymentCreatedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the associated shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number of the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID requesting the payment.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the payment method configured for this transaction.
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total charge amount for the shipment payment.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the payment order was initialized.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}