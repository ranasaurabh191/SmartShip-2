namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a shipment is successfully delivered to the recipient address.
/// Dispatches payload to AdminService to update delivery performance metrics.
/// </summary>
public class ShipmentDeliveredEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the delivered shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number of the delivered shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID owning the shipment.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when final delivery was confirmed.
    /// </summary>
    public DateTime DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the destination location or hub where delivery was recorded.
    /// </summary>
    public string? Location { get; set; }
}