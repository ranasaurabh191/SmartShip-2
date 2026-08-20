namespace SmartShip.Payment.Domain.Entities.Enums;

/// <summary>
/// Enumeration representing the fulfillment state of a payment transaction.
/// </summary>
public enum PaymentStatus 
{ 
    Pending, 
    Paid, 
    Failed, 
    Refunded 
}