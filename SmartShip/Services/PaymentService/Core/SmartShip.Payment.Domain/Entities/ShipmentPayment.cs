using SmartShip.Payment.Domain.Entities.Enums;

namespace SmartShip.Payment.Domain.Entities;

/// <summary>
/// Domain entity representing a payment transaction associated with a shipment.
/// Stores payment amount, status, gateway reference tokens (Razorpay Order/Payment ID), and transaction timestamps.
/// </summary>
public class ShipmentPayment
{
   
    public int Id { get; set; } 
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}