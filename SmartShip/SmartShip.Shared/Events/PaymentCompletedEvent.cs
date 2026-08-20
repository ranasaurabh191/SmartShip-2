namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a payment transaction for a shipment completes successfully.
/// Carries invoice and Razorpay reference details to update shipment status and trigger notification processes.
/// </summary>
public class PaymentCompletedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the shipment.
    /// </summary>
    public int ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number of the shipment.
    /// </summary>
    public string TrackingNumber { get; set; } = "";

    /// <summary>
    /// Gets or sets the payment method used (e.g. CARD, UPI, NETBANKING).
    /// </summary>
    public string PaymentMethod { get; set; } = "";

    /// <summary>
    /// Gets or sets the resulting payment status string (e.g. COMPLETED).
    /// </summary>
    public string PaymentStatus { get; set; } = "";

    /// <summary>
    /// Gets or sets the customer ID owning the shipment.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the monetary amount paid for the shipment.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the formatted timestamp when payment was confirmed.
    /// </summary>
    public string? PaidAt { get; set; }

    /// <summary>
    /// Gets or sets the unique transaction ID returned by the Razorpay payment gateway.
    /// </summary>
    public string? RazorpayPaymentId { get; set; }

    /// <summary>
    /// Gets or sets the order ID created on Razorpay servers.
    /// </summary>
    public string? RazorpayOrderId { get; set; }
}