using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Shipment.Core.DTOs;
using SmartShip.Shipment.Core.Interfaces.Persistence;
using SmartShip.Shipment.Core.Interfaces.Repositories;
using SmartShip.Shipment.Domain.Entities;
using SmartShip.Shipment.Domain.Enums;
using SmartShip.Shipment.Core.Services;
using Xunit;

namespace SmartShip.Shipment.Tests.Services;

public class ShipmentServiceTests
{
    private readonly Mock<IShipmentRepository> _shipmentRepository;
    private readonly Mock<IAddressRepository> _addressRepository;
    private readonly Mock<IPackageRepository> _packageRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<ShipmentService>> _logger;
    private readonly Mock<IPublishEndpoint> _publisher;
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly IConfiguration _configuration;

    private readonly ShipmentService _service;

    public ShipmentServiceTests()
    {
        _shipmentRepository = new Mock<IShipmentRepository>();
        _addressRepository = new Mock<IAddressRepository>();
        _packageRepository = new Mock<IPackageRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<ShipmentService>>();
        _publisher = new Mock<IPublishEndpoint>();
        _httpClientFactory = new Mock<IHttpClientFactory>();

        var configurationData = new Dictionary<string, string?>
        {
            ["InternalApi:ApiKey"] = "test-api-key"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new ShipmentService(
            _shipmentRepository.Object,
            _addressRepository.Object,
            _packageRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            _publisher.Object,
            _httpClientFactory.Object,
            _configuration);
    }

    private static Address CreateAddress(
        string name = "John Doe",
        string city = "Delhi")
    {
        return new Address
        {
            Id = 1,
            FullName = name,
            Phone = "9876543210",
            Street = "123 Main Street",
            City = city,
            State = "Delhi",
            PostalCode = "110001",
            Country = "India"
        };
    }

    private static Package CreatePackage()
    {
        return new Package
        {
            Id = 1,
            WeightKg = 5,
            LengthCm = 20,
            WidthCm = 15,
            HeightCm = 10,
            Description = "Electronics"
        };
    }

    private static Shipments CreateShipment(
        int id = 1,
        int customerId = 100,
        ShipmentStatus status = ShipmentStatus.Draft)
    {
        return new Shipments
        {
            Id = id,
            TrackingNumber = "SHP-20260815-1234",
            CustomerId = customerId,
            ShipmentType = ShipmentType.Domestic,
            Status = status,
            ShippingRate = 400,
            IsFragile = false,
            CreatedAt = DateTime.Now,
            SenderAddress = CreateAddress("Sender", "Delhi"),
            ReceiverAddress = CreateAddress("Receiver", "Mumbai"),
            Package = CreatePackage()
        };
    }

    private static CreateShipmentRequest CreateRequest(
        ShipmentType shipmentType = ShipmentType.Domestic)
    {
        return new CreateShipmentRequest(
            new AddressDto(
                "John Doe",
                "9876543210",
                "Street 1",
                "Delhi",
                "Delhi",
                "110001",
                "India"),
            new AddressDto(
                "Jane Doe",
                "9876543211",
                "Street 2",
                "Mumbai",
                "Maharashtra",
                "400001",
                "India"),
            new PackageDto(
                5,
                20,
                15,
                10,
                "Electronics"),
            shipmentType,            
            "Test shipment",
            false);
    }

    [Theory]
    [InlineData(5, ShipmentType.Domestic, 400)]
    [InlineData(5, ShipmentType.Express, 750)]
    [InlineData(5, ShipmentType.International, 1500)]
    [InlineData(5, ShipmentType.Freight, 250)]
    public async Task CalculateRateAsync_ShouldCalculateCorrectRate(
        double weight,
        ShipmentType type,
        decimal expected)
    {
        var result = await _service.CalculateRateAsync(weight, type);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, ShipmentType.Domestic)]
    [InlineData(0.5, ShipmentType.Domestic)]
    [InlineData(0.1, ShipmentType.Express)]
    public async Task CalculateRateAsync_ShouldApplyMinimumRate(
        double weight,
        ShipmentType type)
    {
        var result = await _service.CalculateRateAsync(weight, type);

        Assert.Equal(99, result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenShipmentExists_ShouldReturnShipment()
    {
        var shipment = CreateShipment();

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(shipment.Id, result.Id);
        Assert.Equal(shipment.TrackingNumber, result.TrackingNumber);
        Assert.Equal(shipment.CustomerId, result.CustomerId);
        Assert.Equal("Domestic", result.ShipmentType);
        Assert.Equal("Draft", result.Status);
        Assert.Equal(shipment.ShippingRate, result.ShippingRate);
        Assert.Equal(shipment.SenderAddress.FullName, result.SenderAddress.FullName);
        Assert.Equal(shipment.ReceiverAddress.FullName, result.ReceiverAddress.FullName);
        Assert.Equal(shipment.Package.WeightKg, result.Package.WeightKg);
    }

    [Fact]
    public async Task GetByIdAsync_WhenShipmentDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _shipmentRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Shipments?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_WhenTrackingNumberIsEmpty_ShouldReturnNull()
    {
        var result = await _service.GetByTrackingNumberAsync("");

        Assert.Null(result);

        _shipmentRepository.Verify(
            x => x.GetByTrackingNumberAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_WhenTrackingNumberIsWhitespace_ShouldReturnNull()
    {
        var result = await _service.GetByTrackingNumberAsync("   ");

        Assert.Null(result);

        _shipmentRepository.Verify(
            x => x.GetByTrackingNumberAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_WhenShipmentExists_ShouldReturnResponse()
    {
        var shipment = CreateShipment();

        _shipmentRepository
            .Setup(x => x.GetByTrackingNumberAsync("SHP-20260815-1234"))
            .ReturnsAsync(shipment);

        var result =
            await _service.GetByTrackingNumberAsync("SHP-20260815-1234");

        Assert.NotNull(result);
        Assert.Equal(shipment.Id, result.Id);
        Assert.Equal(shipment.TrackingNumber, result.TrackingNumber);
        Assert.Equal("Domestic", result.ShipmentType);
    }

    [Fact]
    public async Task GetByTrackingNumberAsync_WhenShipmentDoesNotExist_ShouldReturnNull()
    {
        _shipmentRepository
            .Setup(x => x.GetByTrackingNumberAsync("INVALID"))
            .ReturnsAsync((Shipments?)null);

        var result =
            await _service.GetByTrackingNumberAsync("INVALID");

        Assert.Null(result);
    }

    [Fact]
    public async Task CancelByCustomerAsync_WhenShipmentDoesNotExist_ShouldThrow()
    {
        _shipmentRepository
            .Setup(x => x.GetByIdAndCustomerAsync(1, 100))
            .ReturnsAsync((Shipments?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CancelByCustomerAsync(
                1,
                100,
                "Changed my mind"));
    }

    [Theory]
    [InlineData(ShipmentStatus.PickedUp)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.OutForDelivery)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task CancelByCustomerAsync_WhenStatusDoesNotAllowCancellation_ShouldThrow(
        ShipmentStatus status)
    {
        var shipment = CreateShipment(
            status: status);

        _shipmentRepository
            .Setup(x => x.GetByIdAndCustomerAsync(1, 100))
            .ReturnsAsync(shipment);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CancelByCustomerAsync(
                1,
                100,
                "Customer cancellation"));
    }

    [Theory]
    [InlineData(ShipmentStatus.Draft)]
    [InlineData(ShipmentStatus.Booked)]
    public async Task CancelByCustomerAsync_WhenStatusIsValid_ShouldCancelShipment(
        ShipmentStatus status)
    {
        var shipment = CreateShipment(
            status: status);

        _shipmentRepository
            .Setup(x => x.GetByIdAndCustomerAsync(1, 100))
            .ReturnsAsync(shipment);

        await _service.CancelByCustomerAsync(
            1,
            100,
            "Customer changed mind");

        Assert.Equal(
            ShipmentStatus.Cancelled,
            shipment.Status);

        Assert.Equal(
            "Cancelled by customer: Customer changed mind",
            shipment.Notes);

        _shipmentRepository.Verify(
            x => x.Update(shipment),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenShipmentDoesNotExist_ShouldThrow()
    {
        _shipmentRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Shipments?)null);

        var request = new UpdateStatusRequest
        {
            Status = "Booked"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateStatusAsync(999, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusIsInvalid_ShouldThrowArgumentException()
    {
        var shipment = CreateShipment();

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "INVALID_STATUS"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateStatusAsync(1, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenSameStatusIsRequested_ShouldDoNothing()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.Draft);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "Draft"
        };

        await _service.UpdateStatusAsync(1, request);

        _shipmentRepository.Verify(
            x => x.Update(It.IsAny<Shipments>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelDeliveredShipment_ShouldThrow()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.Delivered);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "Cancelled"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(1, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_InTransit_ShouldThrow()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.PickedUp);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "InTransit"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(1, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_OutForDelivery_ShouldThrow()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.InTransit);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "OutForDelivery"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(1, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_PickedUpBeforeBooked_ShouldThrow()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.Draft);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "PickedUp"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(1, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_BookedWithoutPickup_ShouldThrow()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.Draft);

        shipment.PickupScheduledAt = null;

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "Booked"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(1, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_DeliveredBeforeOutForDelivery_ShouldThrow()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.InTransit);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "Delivered"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStatusAsync(1, request));
    }

    [Fact]
    public async Task UpdateStatusAsync_BookedShipmentToPickedUp_ShouldUpdateStatus()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.Booked);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "PickedUp",
            Location = "Delhi Hub"
        };

        await _service.UpdateStatusAsync(1, request);

        Assert.Equal(
            ShipmentStatus.PickedUp,
            shipment.Status);

        Assert.NotNull(shipment.UpdatedAt);

        _shipmentRepository.Verify(
            x => x.Update(shipment),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_DeliveredShipment_ShouldSetDeliveredAt()
    {
        var shipment = CreateShipment(
            status: ShipmentStatus.OutForDelivery);

        _shipmentRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(shipment);

        var request = new UpdateStatusRequest
        {
            Status = "Delivered",
            Location = "Mumbai"
        };

        await _service.UpdateStatusAsync(1, request);

        Assert.Equal(
            ShipmentStatus.Delivered,
            shipment.Status);

        Assert.NotNull(shipment.DeliveredAt);
        Assert.NotNull(shipment.UpdatedAt);

        _shipmentRepository.Verify(
            x => x.Update(shipment),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SchedulePickupAsync_WhenShipmentDoesNotExist_ShouldThrow()
    {
        _shipmentRepository
            .Setup(x => x.GetByIdAndCustomerAsync(1, 100))
            .ReturnsAsync((Shipments?)null);

        var request = new SchedulePickupRequest
        {
            PickupTime = DateTime.Now.AddDays(1)
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SchedulePickupAsync(
                1,
                100,
                request));
    }

    [Theory]
    [InlineData(ShipmentStatus.PickedUp)]
    [InlineData(ShipmentStatus.Booked)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Cancelled)]
    public async Task SchedulePickupAsync_WhenStatusIsInvalid_ShouldThrow(
        ShipmentStatus status)
    {
        var shipment = CreateShipment(
            status: status);

        _shipmentRepository
            .Setup(x => x.GetByIdAndCustomerAsync(1, 100))
            .ReturnsAsync(shipment);

        var request = new SchedulePickupRequest
        {
            PickupTime = DateTime.Now.AddDays(1)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SchedulePickupAsync(
                1,
                100,
                request));
    }

    [Theory]
    [InlineData(ShipmentStatus.Draft)]
    [InlineData(ShipmentStatus.PaymentFailed)]
    public async Task SchedulePickupAsync_WhenStatusIsValid_ShouldBookShipment(
        ShipmentStatus status)
    {
        var pickupTime = DateTime.Now.AddDays(1);

        var shipment = CreateShipment(
            status: status);

        _shipmentRepository
            .Setup(x => x.GetByIdAndCustomerAsync(1, 100))
            .ReturnsAsync(shipment);

        var request = new SchedulePickupRequest
        {
            PickupTime = pickupTime
        };

        await _service.SchedulePickupAsync(
            1,
            100,
            request);

        Assert.Equal(
            ShipmentStatus.Booked,
            shipment.Status);

        Assert.Equal(
            pickupTime,
            shipment.PickupScheduledAt);

        Assert.Equal(
            pickupTime,
            shipment.UpdatedAt);

        _shipmentRepository.Verify(
            x => x.Update(shipment),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerDoesNotExist_ShouldThrow()
    {
        var handler = new TestHttpMessageHandler(
            System.Net.HttpStatusCode.NotFound);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5002")
        };

        _httpClientFactory
            .Setup(x => x.CreateClient("IdentityService"))
            .Returns(client);

        var request = CreateRequest();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateAsync(request, 100));

        _addressRepository.Verify(
            x => x.AddRangeAsync(
                It.IsAny<Address>(),
                It.IsAny<Address>()),
            Times.Never);

        _packageRepository.Verify(
            x => x.AddAsync(It.IsAny<Package>()),
            Times.Never);

        _shipmentRepository.Verify(
            x => x.AddAsync(It.IsAny<Shipments>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerExists_ShouldCreateShipment()
    {
        var handler = new TestHttpMessageHandler(
            System.Net.HttpStatusCode.OK);

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5002")
        };

        _httpClientFactory
            .Setup(x => x.CreateClient("IdentityService"))
            .Returns(client);

        _addressRepository
            .Setup(x => x.AddRangeAsync(It.IsAny<Address[]>()))
            .Callback<Address[]>(addresses =>
            {
                addresses[0].Id = 1;
                addresses[1].Id = 2;
            })
            .Returns(Task.CompletedTask);

        _packageRepository
            .Setup(x => x.AddAsync(It.IsAny<Package>()))
            .Callback<Package>(package =>
            {
                package.Id = 3;
            })
            .Returns(Task.CompletedTask);

        _shipmentRepository
            .Setup(x => x.AddAsync(It.IsAny<Shipments>()))
            .Callback<Shipments>(shipment =>
            {
                shipment.Id = 10;
            })
            .Returns(Task.CompletedTask);

        var request = CreateRequest(
            ShipmentType.Domestic);

        var result = await _service.CreateAsync(
            request,
            100);

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal(100, result.CustomerId);
        Assert.Equal("Domestic", result.ShipmentType);
        Assert.Equal("Draft", result.Status);
        Assert.Equal(400, result.ShippingRate);
        Assert.Equal("Pending", result.PaymentStatus);

        Assert.StartsWith(
            "SHP-",
            result.TrackingNumber);

        _addressRepository.Verify(
            x => x.AddRangeAsync(
                It.IsAny<Address>(),
                It.IsAny<Address>()),
            Times.Once);

        _packageRepository.Verify(
            x => x.AddAsync(It.IsAny<Package>()),
            Times.Once);

        _shipmentRepository.Verify(
            x => x.AddAsync(It.Is<Shipments>(s =>
                s.CustomerId == 100 &&
                s.Status == ShipmentStatus.Draft &&
                s.ShipmentType == ShipmentType.Domestic)),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly System.Net.HttpStatusCode _statusCode;

        public TestHttpMessageHandler(
            System.Net.HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new HttpResponseMessage(_statusCode));
        }
    }
}