namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a customer directly requests and confirms a shipment cancellation via the customer portal.
/// Notifies PaymentService to handle refund processing if the shipment was previously paid.
/// </summary>
public class ShipmentCancelledByCustomerEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the cancelled shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number of the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID who cancelled the shipment.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the monetary amount associated with the shipment.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the shipment had completed payment prior to cancellation.
    /// </summary>
    public bool WasPaid { get; set; }       

    /// <summary>
    /// Gets or sets the timestamp when the cancellation was executed.
    /// </summary>
    public DateTime CancelledAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the user-provided reason for cancelling the shipment.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}