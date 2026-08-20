namespace SmartShip.Payment.Application.DTOs;

/// <summary>
/// Request DTO for validating Razorpay payment signatures upon checkout completion.
/// </summary>
public class VerifyPaymentRequest
{ 
    public string RazorpayOrderId { get; set; } = "";
    public string RazorpayPaymentId { get; set; } = "";
    public string Signature { get; set; } = "";
    public int? ShipmentId { get; set; }        
}