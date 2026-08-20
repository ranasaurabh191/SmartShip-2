namespace SmartShip.Payment.Application.DTOs;

/// <summary>
/// Response DTO for sandbox/demo payment flows containing generated test tokens and signature mock.
/// </summary>
public class DemoPaymentResponse
{

    public string RazorpayOrderId { get; set; } = "";

    public string RazorpayPaymentId { get; set; } = "";

    public string Signature { get; set; } = "";

    public string Message { get; set; } = "";
}