namespace SmartShip.Shared.Events;

/// <summary>
/// Command sent via MassTransit to request the cancellation of a specific shipment.
/// Typically dispatched when an admin or customer triggers a cancellation workflow.
/// </summary>
public class CancelShipmentCommand
{
    /// <summary>
    /// Gets or sets the unique database identifier of the shipment to be cancelled.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number of the target shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of the customer requesting or associated with the cancellation.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the reason or justification provided for the cancellation request.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}