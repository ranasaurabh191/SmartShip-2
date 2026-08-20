namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published whenever a shipment's status advances through the fulfillment lifecycle
/// (e.g., CREATED -> PICKED_UP -> IN_TRANSIT -> OUT_FOR_DELIVERY -> DELIVERED).
/// </summary>
public class ShipmentStatusUpdatedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number assigned to the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous status string prior to this update.
    /// </summary>
    public string OldStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new status string applied to the shipment.
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current physical location or hub where the status change was scanned.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username, driver ID, or service component that performed the status update.
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the status update occurred.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the customer ID owning the shipment.
    /// </summary>
    public int CustomerId { get; set; }
}