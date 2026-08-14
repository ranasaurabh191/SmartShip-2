using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Payment.API.Controllers;
using SmartShip.Payment.Application.DTOs;
using SmartShip.Payment.Application.Interfaces.Services;
using SmartShip.Payment.Domain.Entities.Enums;
using Xunit;

namespace SmartShip.Payment.Tests.Controllers;

public class PaymentControllerTests
{
    private readonly Mock<IPaymentService> _paymentService;
    private readonly Mock<ILogger<PaymentController>> _logger;
    private readonly PaymentController _controller;

    public PaymentControllerTests()
    {
        _paymentService = new Mock<IPaymentService>();
        _logger = new Mock<ILogger<PaymentController>>();

        _controller = new PaymentController(
            _paymentService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnOk()
    {
        var request = new CreateOrderRequest(
            1,
            PaymentMethod.Online);

        var response = new PaymentResponse
        {
            Id = 1,
            ShipmentId = 1,
            TrackingNumber = "TRK001",
            Amount = 1298m,
            PaymentMethod = "Online",
            PaymentStatus = "Pending",
            RazorpayOrderId = "order_123"
        };

        _paymentService
            .Setup(x => x.CreateOrderAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.CreateOrder(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _paymentService.Verify(
            x => x.CreateOrderAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task Verify_ShouldReturnOk()
    {
        var request = new VerifyPaymentRequest
        {
            RazorpayOrderId = "order_123",
            RazorpayPaymentId = "pay_123",
            Signature = "signature",
            ShipmentId = 1,
            PaymentMethod = "Online"
        };

        var response = new PaymentResponse
        {
            Id = 1,
            ShipmentId = 1,
            PaymentStatus = "Paid",
            PaymentMethod = "Online",
            RazorpayOrderId = "order_123",
            RazorpayPaymentId = "pay_123"
        };

        _paymentService
            .Setup(x => x.VerifyPaymentAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.Verify(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _paymentService.Verify(
            x => x.VerifyPaymentAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task PaymentStatus_ShouldReturnOk()
    {
        var response = new PaymentResponse
        {
            Id = 1,
            ShipmentId = 1,
            TrackingNumber = "TRK001",
            PaymentStatus = "Paid",
            PaymentMethod = "Online"
        };

        _paymentService
            .Setup(x => x.PaymentStatusAsync(
                It.Is<PaymentStatusRequest>(r =>
                    r.RazorpayOrderId == "order_123" &&
                    r.ShipmentId == 1 &&
                    r.TrackingNumber == "TRK001")))
            .ReturnsAsync(response);

        var result = await _controller.PaymentStatus(
            "order_123",
            1,
            "TRK001");

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _paymentService.Verify(
            x => x.PaymentStatusAsync(
                It.Is<PaymentStatusRequest>(r =>
                    r.RazorpayOrderId == "order_123" &&
                    r.ShipmentId == 1 &&
                    r.TrackingNumber == "TRK001")),
            Times.Once);
    }

    [Fact]
    public async Task GetByShipment_ShouldReturnOk()
    {
        var response = new PaymentResponse
        {
            Id = 1,
            ShipmentId = 100,
            TrackingNumber = "TRK100",
            Amount = 1500m,
            PaymentMethod = "COD",
            PaymentStatus = "Pending"
        };

        _paymentService
            .Setup(x => x.GetByShipmentIdAsync(100))
            .ReturnsAsync(response);

        var result = await _controller.GetByShipment(100);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(response, okResult.Value);

        _paymentService.Verify(
            x => x.GetByShipmentIdAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task GetMyPayments_ShouldReturnOk()
    {
        var payments = new List<PaymentResponse>
        {
            new()
            {
                Id = 1,
                ShipmentId = 1,
                TrackingNumber = "TRK001",
                Amount = 1000m,
                PaymentMethod = "COD",
                PaymentStatus = "Pending"
            },
            new()
            {
                Id = 2,
                ShipmentId = 2,
                TrackingNumber = "TRK002",
                Amount = 2000m,
                PaymentMethod = "Online",
                PaymentStatus = "Paid"
            }
        };

        _paymentService
            .Setup(x => x.GetMyPaymentsAsync())
            .ReturnsAsync(payments);

        var result = await _controller.GetMyPayments();

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(payments, okResult.Value);
        Assert.Equal(2, payments.Count);

        _paymentService.Verify(
            x => x.GetMyPaymentsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllPayments_ShouldReturnOk()
    {
        var payments = new List<PaymentResponse>
        {
            new()
            {
                Id = 1,
                ShipmentId = 1,
                TrackingNumber = "TRK001",
                Amount = 1000m,
                PaymentMethod = "COD",
                PaymentStatus = "Pending"
            },
            new()
            {
                Id = 2,
                ShipmentId = 2,
                TrackingNumber = "TRK002",
                Amount = 2000m,
                PaymentMethod = "Online",
                PaymentStatus = "Paid"
            }
        };

        _paymentService
            .Setup(x => x.GetAllPaymentsAsync())
            .ReturnsAsync(payments);

        var result = await _controller.GetAllPayments();

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Same(payments, okResult.Value);
        Assert.Equal(2, payments.Count);

        _paymentService.Verify(
            x => x.GetAllPaymentsAsync(),
            Times.Once);
    }
}