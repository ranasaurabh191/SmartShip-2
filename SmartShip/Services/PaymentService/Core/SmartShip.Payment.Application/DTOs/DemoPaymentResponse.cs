namespace SmartShip.Payment.Application.DTOs;

public class DemoPaymentResponse
{
    public string RazorpayOrderId { get; set; } = "";
    public string RazorpayPaymentId { get; set; } = "";
    public string Signature { get; set; } = "";
    public string Message { get; set; } = "";
}