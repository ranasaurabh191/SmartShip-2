namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a new shipment record is created in ShipmentService.
/// Dispatches payload to AdminService (for metrics tracking) and PaymentService (for billing initialization).
/// </summary>
public class ShipmentCreatedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the newly created shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the generated tracking number assigned to the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID owning the shipment.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the origin sender city for routing and reporting analytics.
    /// </summary>
    public string SenderCity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the shipment record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the total calculated shipping cost.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package contains fragile items requiring special handling.
    /// </summary>
    public bool IsFragile { get; set; }
}