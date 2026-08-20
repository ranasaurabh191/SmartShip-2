namespace SmartShip.Shared.Events;

/// <summary>
/// Event published across the message bus (MassTransit/RabbitMQ) when a shipment is successfully cancelled in the system.
/// Notifies downstream services (such as PaymentService and AdminService) to update payment status and system metrics.
/// </summary>
public class ShipmentCancelledEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the cancelled shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the unique tracking number assigned to the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = "";

    /// <summary>
    /// Gets or sets the timestamp when the cancellation occurred.
    /// </summary>
    public DateTime CancelledAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the unique identifier of the customer who owned the cancelled shipment.
    /// </summary>
    public int CustomerId { get; set; }
}