using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.Payment.Application.DTOs;
using SmartShip.Payment.Application.Interfaces.Services;


namespace SmartShip.Payment.API.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;
    private readonly IRazorpayClient _razorpayClient;

    public PaymentController(
    IPaymentService paymentService,
    IRazorpayClient razorpayClient,
    ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _razorpayClient = razorpayClient;
        _logger = logger;
    }

    [Authorize(Roles = "CUSTOMER")]
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Create order request for Shipment {ShipmentId} | Method: {Method}", request.ShipmentId, request.PaymentMethod);
        var result = await _paymentService.CreateOrderAsync(request);
        return Ok(result);
    }

    [Authorize(Roles = "CUSTOMER")]
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentRequest request)
    {
        _logger.LogInformation("Verify payment request for Order {OrderId}", request.RazorpayOrderId);
        var result = await _paymentService.VerifyPaymentAsync(request);
        return Ok(result);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("payment-status")]
    public async Task<IActionResult> PaymentStatus(
    [FromQuery] string? razorpayOrderId,
    [FromQuery] int? shipmentId,
    [FromQuery] string? trackingNumber)
    {
        _logger.LogInformation("Payment status request | OrderId:{OrderId} | ShipmentId:{ShipmentId} | Tracking:{Tracking}",
            razorpayOrderId, shipmentId, trackingNumber);

        var request = new PaymentStatusRequest
        {
            RazorpayOrderId = razorpayOrderId,
            ShipmentId = shipmentId,
            TrackingNumber = trackingNumber
        };

        var result = await _paymentService.PaymentStatusAsync(request);
        return Ok(result);
    }

    [HttpGet("shipment/{shipmentId}")]
    public async Task<IActionResult> GetByShipment(int shipmentId)
    {
        _logger.LogInformation("Fetching payment for Shipment {ShipmentId}", shipmentId);
        var result = await _paymentService.GetByShipmentIdAsync(shipmentId);
        return Ok(result);
    }

    [HttpGet("my")]
    [Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetMyPayments()
    {
        var result = await _paymentService.GetMyPaymentsAsync();
        return Ok(result);
    }

    [HttpGet("all")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllPayments()
    {
        var result = await _paymentService.GetAllPaymentsAsync();
        return Ok(result);
    }

    [Authorize(Roles = "CUSTOMER")]
    [HttpPost("demo-payment/{orderId}")]
    public IActionResult DemoPayment(string orderId)
    {
        var paymentId = _razorpayClient.GenerateDemoPaymentId();

        var signature = _razorpayClient.GenerateDemoSignature(
            orderId,
            paymentId);

        return Ok(new DemoPaymentResponse
        {
            RazorpayOrderId = orderId,
            RazorpayPaymentId = paymentId,
            Signature = signature,
            Message = "Demo payment generated successfully."
        });
    }
}