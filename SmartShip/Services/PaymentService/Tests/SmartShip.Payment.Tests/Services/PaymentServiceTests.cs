using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Payment.Application.DTOs;
using SmartShip.Payment.Application.Interfaces.Services;
using SmartShip.Payment.Application.Repositories;
using SmartShip.Payment.Core.Services;
using SmartShip.Payment.Domain.Entities;
using SmartShip.Payment.Domain.Entities.Enums;
using SmartShip.Shared.Events;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SmartShip.Payment.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IPublishEndpoint> _publisher;
    private readonly Mock<ILogger<PaymentService>> _logger;
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IRazorpayClient> _razorpayClient;

    public PaymentServiceTests()
    {
        _paymentRepository = new Mock<IPaymentRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _publisher = new Mock<IPublishEndpoint>();
        _logger = new Mock<ILogger<PaymentService>>();
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _razorpayClient = new Mock<IRazorpayClient>();

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private PaymentService CreateService()
    {
        return new PaymentService(
            _paymentRepository.Object,
            _unitOfWork.Object,
            _publisher.Object,
            _logger.Object,
            _httpClientFactory.Object,
            _httpContextAccessor.Object,
            _razorpayClient.Object);
    }

    private void SetUser(int userId)
    {
        var claims = new[]
        {
            new Claim("userId", userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(context);
    }

    private void SetUserWithNameIdentifier(int userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(context);
    }

    private void SetupShipmentClient(ShipmentDTOs shipment, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler((request, cancellationToken) =>
        {
            var json = JsonSerializer.Serialize(shipment);

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5004")
        };

        _httpClientFactory
            .Setup(x => x.CreateClient("ShipmentService"))
            .Returns(client);
    }

    private static ShipmentDTOs CreateShipment(
        int shipmentId = 1,
        int customerId = 10,
        decimal shippingRate = 1000m,
        string shipmentType = "Domestic",
        bool isFragile = false,
        double senderLat = 28.6139,
        double senderLng = 77.2090,
        double receiverLat = 28.6139,
        double receiverLng = 77.2090)
    {
        return new ShipmentDTOs
        {
            Id = shipmentId,
            TrackingNumber = "TRK001",
            CustomerId = customerId,
            ShippingRate = shippingRate,
            ShipmentType = shipmentType,
            IsFragile = isFragile,
            SenderLat = senderLat,
            SenderLng = senderLng,
            ReceiverLat = receiverLat,
            ReceiverLng = receiverLng
        };
    }

    private static ShipmentPayment CreatePayment(
        int shipmentId = 1,
        int customerId = 10,
        PaymentStatus status = PaymentStatus.Pending,
        PaymentMethod method = PaymentMethod.Online)
    {
        return new ShipmentPayment
        {
            Id = 1,
            ShipmentId = shipmentId,
            TrackingNumber = "TRK001",
            CustomerId = customerId,
            Amount = 1298m,
            PaymentMethod = method,
            PaymentStatus = status,
            RazorpayOrderId = "order_123",
            CreatedAt = DateTime.Now
        };
    }

    [Fact]
    public async Task CreateOrderAsync_WhenUserIsUnauthorized_ShouldThrowUnauthorizedAccessException()
    {
        var request = new CreateOrderRequest(1, PaymentMethod.Online);

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(new DefaultHttpContext());

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_WhenShipmentDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        SetUser(10);

        SetupShipmentClient(
            CreateShipment(),
            HttpStatusCode.NotFound);

        var request = new CreateOrderRequest(1, PaymentMethod.Online);

        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_WhenShipmentBelongsToAnotherUser_ShouldThrowUnauthorizedAccessException()
    {
        SetUser(10);

        var shipment = CreateShipment(customerId: 20);

        SetupShipmentClient(shipment);

        var request = new CreateOrderRequest(1, PaymentMethod.Online);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_Online_ShouldCreateRazorpayOrder()
    {
        SetUser(10);

        var shipment = CreateShipment(
            shippingRate: 1000m,
            shipmentType: "Domestic",
            isFragile: false);

        SetupShipmentClient(shipment);

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync((ShipmentPayment?)null);

        _razorpayClient
            .Setup(x => x.CreateOrder(1298m, 1))
            .Returns("order_test_123");

        var request = new CreateOrderRequest(1, PaymentMethod.Online);

        var service = CreateService();

        var result = await service.CreateOrderAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Online", result.PaymentMethod);
        Assert.Equal("Pending", result.PaymentStatus);
        Assert.Equal(1298m, result.Amount);
        Assert.Equal("order_test_123", result.RazorpayOrderId);
        Assert.Equal(
            "Online payment order created. Please complete payment.",
            result.Message);

        _razorpayClient.Verify(
            x => x.CreateOrder(1298m, 1),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenRazorpayFails_ShouldThrowInvalidOperationException()
    {
        SetUser(10);

        SetupShipmentClient(CreateShipment());

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync((ShipmentPayment?)null);

        _razorpayClient
            .Setup(x => x.CreateOrder(It.IsAny<decimal>(), 1))
            .Throws(new Exception("Razorpay unavailable"));

        var request = new CreateOrderRequest(1, PaymentMethod.Online);

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Equal(
            "Failed to initiate payment. Please try again.",
            exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenExistingPaymentIsPaid_ShouldThrowInvalidOperationException()
    {
        SetUser(10);

        SetupShipmentClient(CreateShipment());

        var payment = CreatePayment(
            status: PaymentStatus.Paid);

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync(payment);

        var request = new CreateOrderRequest(1, PaymentMethod.Online);

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Equal(
            "You have already paid for this shipment.",
            exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenExistingPendingPayment_ShouldUpdatePayment()
    {
        SetUser(10);

        SetupShipmentClient(CreateShipment());

        var payment = CreatePayment(
            status: PaymentStatus.Pending);

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync(payment);

        _razorpayClient
            .Setup(x => x.CreateOrder(It.IsAny<decimal>(), 1))
            .Returns("new_order");

        var request = new CreateOrderRequest(1, PaymentMethod.Online);

        var service = CreateService();

        var result = await service.CreateOrderAsync(request);

        Assert.Equal("new_order", result.RazorpayOrderId);

        _paymentRepository.Verify(
            x => x.Update(It.Is<ShipmentPayment>(p =>
                p.ShipmentId == 1 &&
                p.PaymentMethod == PaymentMethod.Online &&
                p.PaymentStatus == PaymentStatus.Pending)),
            Times.Once);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenUserIsUnauthorized_ShouldThrowUnauthorizedAccessException()
    {
        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(new DefaultHttpContext());

        var request = new VerifyPaymentRequest
        {
            RazorpayOrderId = "order_123",
            RazorpayPaymentId = "pay_123",
            Signature = "signature"
        };

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.VerifyPaymentAsync(request));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenOrderDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        SetUser(10);

        _paymentRepository
            .Setup(x => x.GetByOrderIdAsync("invalid_order"))
            .ReturnsAsync((ShipmentPayment?)null);

        var request = new VerifyPaymentRequest
        {
            RazorpayOrderId = "invalid_order",
            RazorpayPaymentId = "pay_123",
            Signature = "signature",
            ShipmentId = 1
        };

        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.VerifyPaymentAsync(request));

        _publisher.Verify(
            x => x.Publish(
                It.IsAny<PaymentFailedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentBelongsToAnotherUser_ShouldThrowUnauthorizedAccessException()
    {
        SetUser(10);

        var payment = CreatePayment(customerId: 20);

        _paymentRepository
            .Setup(x => x.GetByOrderIdAsync("order_123"))
            .ReturnsAsync(payment);

        var request = new VerifyPaymentRequest
        {
            RazorpayOrderId = "order_123",
            RazorpayPaymentId = "pay_123",
            Signature = "signature"
        };

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.VerifyPaymentAsync(request));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenAlreadyPaid_ShouldThrowInvalidOperationException()
    {
        SetUser(10);

        var payment = CreatePayment(
            customerId: 10,
            status: PaymentStatus.Paid);

        _paymentRepository
            .Setup(x => x.GetByOrderIdAsync("order_123"))
            .ReturnsAsync(payment);

        var request = new VerifyPaymentRequest
        {
            RazorpayOrderId = "order_123",
            RazorpayPaymentId = "pay_123",
            Signature = "signature"
        };

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.VerifyPaymentAsync(request));
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenSignatureIsInvalid_ShouldMarkPaymentFailed()
    {
        SetUser(10);

        var payment = CreatePayment(
            customerId: 10,
            status: PaymentStatus.Pending);

        _paymentRepository
            .Setup(x => x.GetByOrderIdAsync("order_123"))
            .ReturnsAsync(payment);

        _razorpayClient
            .Setup(x => x.VerifySignature(
                "order_123",
                "pay_123",
                "invalid"))
            .Returns(false);

        var request = new VerifyPaymentRequest
        {
            RazorpayOrderId = "order_123",
            RazorpayPaymentId = "pay_123",
            Signature = "invalid"
        };

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.VerifyPaymentAsync(request));

        Assert.Equal(
            "Payment signature verification failed. This payment has been flagged.",
            exception.Message);

        Assert.Equal(PaymentStatus.Failed, payment.PaymentStatus);

        _paymentRepository.Verify(
            x => x.Update(payment),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _publisher.Verify(
            x => x.Publish(
                It.IsAny<PaymentFailedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenSignatureIsValid_ShouldMarkPaymentPaid()
    {
        SetUser(10);

        var payment = CreatePayment(
            customerId: 10,
            status: PaymentStatus.Pending);

        _paymentRepository
            .Setup(x => x.GetByOrderIdAsync("order_123"))
            .ReturnsAsync(payment);

        _razorpayClient
            .Setup(x => x.VerifySignature(
                "order_123",
                "pay_123",
                "valid_signature"))
            .Returns(true);

        var request = new VerifyPaymentRequest
        {
            RazorpayOrderId = "order_123",
            RazorpayPaymentId = "pay_123",
            Signature = "valid_signature"
        };

        var service = CreateService();

        var result = await service.VerifyPaymentAsync(request);

        Assert.Equal("Paid", result.PaymentStatus);
        Assert.Equal("Online", result.PaymentMethod);
        Assert.Equal("pay_123", result.RazorpayPaymentId);
        Assert.Equal("valid_signature", payment.RazorpaySignature);
        Assert.NotNull(payment.PaidAt);
        Assert.Equal(PaymentStatus.Paid, payment.PaymentStatus);
        Assert.Equal("Payment successful!", result.Message);

        _paymentRepository.Verify(
            x => x.Update(payment),
            Times.Once);

        _publisher.Verify(
            x => x.Publish(
                It.IsAny<PaymentCompletedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PaymentStatusAsync_WhenSearchingByOrderId_ShouldReturnPayment()
    {
        var payment = CreatePayment();

        _paymentRepository
            .Setup(x => x.GetByOrderIdAsync("order_123"))
            .ReturnsAsync(payment);

        var request = new PaymentStatusRequest
        {
            RazorpayOrderId = "order_123"
        };

        var service = CreateService();

        var result = await service.PaymentStatusAsync(request);

        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.ShipmentId);
        Assert.Equal("TRK001", result.TrackingNumber);
        Assert.Equal("Pending", result.PaymentStatus);
        Assert.Equal("Online", result.PaymentMethod);
        Assert.Equal(
            "Payment initiated. Please complete payment.",
            result.Message);
    }

    [Fact]
    public async Task PaymentStatusAsync_WhenSearchingByShipmentId_ShouldReturnPayment()
    {
        var payment = CreatePayment();

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync(payment);

        var request = new PaymentStatusRequest
        {
            ShipmentId = 1
        };

        var service = CreateService();

        var result = await service.PaymentStatusAsync(request);

        Assert.Equal(1, result.ShipmentId);

        _paymentRepository.Verify(
            x => x.GetByShipmentIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task PaymentStatusAsync_WhenSearchingByTrackingNumber_ShouldReturnPayment()
    {
        var payment = CreatePayment();

        _paymentRepository
            .Setup(x => x.GetByTrackingNumberAsync("TRK001"))
            .ReturnsAsync(payment);

        var request = new PaymentStatusRequest
        {
            TrackingNumber = "TRK001"
        };

        var service = CreateService();

        var result = await service.PaymentStatusAsync(request);

        Assert.Equal("TRK001", result.TrackingNumber);

        _paymentRepository.Verify(
            x => x.GetByTrackingNumberAsync("TRK001"),
            Times.Once);
    }

    [Fact]
    public async Task PaymentStatusAsync_WhenPaymentDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _paymentRepository
            .Setup(x => x.GetByOrderIdAsync("missing"))
            .ReturnsAsync((ShipmentPayment?)null);

        var request = new PaymentStatusRequest
        {
            RazorpayOrderId = "missing"
        };

        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.PaymentStatusAsync(request));
    }

    [Fact]
    public async Task PaymentStatusAsync_WhenCODIsPending_ShouldReturnCODMessage()
    {
        var payment = CreatePayment(
            method: PaymentMethod.COD,
            status: PaymentStatus.Pending);

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync(payment);

        var request = new PaymentStatusRequest
        {
            ShipmentId = 1
        };

        var service = CreateService();

        var result = await service.PaymentStatusAsync(request);

        Assert.Equal(
            "COD registered. Pay on delivery.",
            result.Message);
    }

    [Fact]
    public async Task PaymentStatusAsync_WhenPaymentIsPaid_ShouldReturnSuccessMessage()
    {
        var payment = CreatePayment(
            status: PaymentStatus.Paid);

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync(payment);

        var request = new PaymentStatusRequest
        {
            ShipmentId = 1
        };

        var service = CreateService();

        var result = await service.PaymentStatusAsync(request);

        Assert.Equal(
            "Payment completed successfully.",
            result.Message);
    }

    [Fact]
    public async Task PaymentStatusAsync_WhenPaymentFailed_ShouldReturnFailedMessage()
    {
        var payment = CreatePayment(
            status: PaymentStatus.Failed);

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync(payment);

        var request = new PaymentStatusRequest
        {
            ShipmentId = 1
        };

        var service = CreateService();

        var result = await service.PaymentStatusAsync(request);

        Assert.Equal(
            "Payment failed. Please try again.",
            result.Message);
    }

    [Fact]
    public async Task GetMyPaymentsAsync_WhenUserIsAuthenticated_ShouldReturnCustomerPayments()
    {
        SetUser(10);

        var payments = new List<ShipmentPayment>
        {
            new()
            {
                Id = 1,
                ShipmentId = 1,
                CustomerId = 10,
                TrackingNumber = "TRK001",
                Amount = 1000m,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.Now.AddDays(-1)
            },
            new()
            {
                Id = 2,
                ShipmentId = 2,
                CustomerId = 10,
                TrackingNumber = "TRK002",
                Amount = 2000m,
                PaymentMethod = PaymentMethod.Online,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = DateTime.Now
            }
        };

        _paymentRepository
            .Setup(x => x.GetByCustomerIdAsync(10))
            .ReturnsAsync(payments);

        var service = CreateService();

        var result = await service.GetMyPaymentsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);

        _paymentRepository.Verify(
            x => x.GetByCustomerIdAsync(10),
            Times.Once);
    }

    [Fact]
    public async Task GetMyPaymentsAsync_WhenUserIsNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(new DefaultHttpContext());

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetMyPaymentsAsync());
    }

    [Fact]
    public async Task GetMyPaymentsAsync_ShouldReturnOnlyRepositoryResults()
    {
        SetUserWithNameIdentifier(25);

        var payments = new List<ShipmentPayment>
        {
            new()
            {
                Id = 10,
                ShipmentId = 10,
                CustomerId = 25,
                TrackingNumber = "TRK010",
                Amount = 500m,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Pending
            }
        };

        _paymentRepository
            .Setup(x => x.GetByCustomerIdAsync(25))
            .ReturnsAsync(payments);

        var service = CreateService();

        var result = await service.GetMyPaymentsAsync();

        Assert.Single(result);
        Assert.Equal(10, result[0].Id);
    }

    [Fact]
    public async Task GetAllPaymentsAsync_ShouldReturnPaymentsInDescendingCreatedOrder()
    {
        var oldPayment = new ShipmentPayment
        {
            Id = 1,
            ShipmentId = 1,
            TrackingNumber = "OLD",
            CustomerId = 1,
            Amount = 100,
            PaymentMethod = PaymentMethod.COD,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.Now.AddDays(-2)
        };

        var newPayment = new ShipmentPayment
        {
            Id = 2,
            ShipmentId = 2,
            TrackingNumber = "NEW",
            CustomerId = 2,
            Amount = 200,
            PaymentMethod = PaymentMethod.Online,
            PaymentStatus = PaymentStatus.Paid,
            CreatedAt = DateTime.Now
        };

        _paymentRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<ShipmentPayment>
            {
                oldPayment,
                newPayment
            });

        var service = CreateService();

        var result = await service.GetAllPaymentsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public async Task GetByShipmentIdAsync_WhenPaymentExists_ShouldReturnPayment()
    {
        var payment = CreatePayment();

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync(payment);

        var service = CreateService();

        var result = await service.GetByShipmentIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.ShipmentId);
        Assert.Equal("TRK001", result.TrackingNumber);
        Assert.Equal(1298m, result.Amount);
    }

    [Fact]
    public async Task GetByShipmentIdAsync_WhenPaymentDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(999))
            .ReturnsAsync((ShipmentPayment?)null);

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByShipmentIdAsync(999));

        Assert.Equal(
            "Payment record not found for Shipment 999.",
            exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_InternationalFragileCOD_ShouldCalculateChargesCorrectly()
    {
        SetUser(10);

        var shipment = CreateShipment(
            shippingRate: 1000m,
            shipmentType: "International",
            isFragile: true);

        SetupShipmentClient(shipment);

        _paymentRepository
            .Setup(x => x.GetByShipmentIdAsync(1))
            .ReturnsAsync((ShipmentPayment?)null);

        var request = new CreateOrderRequest(
            1,
            PaymentMethod.COD);

        var service = CreateService();

        var result = await service.CreateOrderAsync(request);

        Assert.Equal(1492.70m, result.Amount);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}