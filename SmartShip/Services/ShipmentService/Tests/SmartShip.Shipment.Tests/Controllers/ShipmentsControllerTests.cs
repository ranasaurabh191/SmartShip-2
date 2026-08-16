using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Shipment.API.Controllers;
using SmartShip.Shipment.Core.DTOs;
using SmartShip.Shipment.Core.Interfaces.Services;
using SmartShip.Shipment.Domain.Enums;
using System.Security.Claims;
using Xunit;

namespace SmartShip.Shipment.Tests.Controllers;

public class ShipmentsControllerTests
{
    private readonly Mock<IShipmentService> _service;
    private readonly Mock<ILogger<ShipmentsController>> _logger;
    private readonly ShipmentsController _controller;

    public ShipmentsControllerTests()
    {
        _service = new Mock<IShipmentService>();
        _logger = new Mock<ILogger<ShipmentsController>>();

        _controller = new ShipmentsController(
            _service.Object,
            _logger.Object);
    }

    private void SetUser(
        int userId,
        string role = "CUSTOMER")
    {
        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new Claim(
                "userId",
                userId.ToString()),

            new Claim(
                ClaimTypes.Role,
                role)
        };

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication");

        _controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
    }

    private static ShipmentResponse CreateResponse()
    {
        return new ShipmentResponse(
            1,
            "SHP-20260815-1234",
            100,
            "Domestic",
            "Draft",
            "Pending",
            400,
            DateTime.Now.ToString("O"),
            null,
            null,
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
                "Electronics"
              ),
            "Test shipment",
            false);
    }

    private static CreateShipmentRequest CreateRequest()
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
                "Electronics"
              ),
            ShipmentType.Domestic, "Test shipment",
            false);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        SetUser(100);

        var request = CreateRequest();
        var response = CreateResponse();

        _service
            .Setup(x => x.CreateAsync(request, 100))
            .ReturnsAsync(response);

        var result = await _controller.Create(request);

        var createdResult =
            Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(
            nameof(ShipmentsController.GetById),
            createdResult.ActionName);

        Assert.Equal(
            201,
            createdResult.StatusCode);

        Assert.Equal(
            response,
            createdResult.Value);

        Assert.Equal(
            1,
            createdResult.RouteValues!["id"]);

        _service.Verify(
            x => x.CreateAsync(request, 100),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        SetUser(100);

        var response = CreateResponse();

        _service
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(response);

        var result =
            await _controller.GetById(1);

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);

        _service.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task SchedulePickup_WithValidUser_ShouldReturnOk()
    {
        SetUser(100);

        var request = new SchedulePickupRequest
        {
            PickupTime = DateTime.Now.AddDays(1)
        };

        _service
            .Setup(x => x.SchedulePickupAsync(
                1,
                100,
                request))
            .Returns(Task.CompletedTask);

        var result =
            await _controller.SchedulePickup(
                1,
                request);

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);

        _service.Verify(
            x => x.SchedulePickupAsync(
                1,
                100,
                request),
            Times.Once);
    }

    [Fact]
    public async Task SchedulePickup_WhenUserIdMissing_ShouldReturnUnauthorized()
    {
        _controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[]
                            {
                                new Claim(
                                    ClaimTypes.Role,
                                    "CUSTOMER")
                            },
                            "TestAuthentication"))
                }
            };

        var request = new SchedulePickupRequest
        {
            PickupTime = DateTime.Now.AddDays(1)
        };

        var result =
            await _controller.SchedulePickup(
                1,
                request);

        var unauthorized =
            Assert.IsType<UnauthorizedObjectResult>(result);

        Assert.Equal(401, unauthorized.StatusCode);

        _service.Verify(
            x => x.SchedulePickupAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<SchedulePickupRequest>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Domestic", 5, 400)]
    [InlineData("Express", 5, 750)]
    [InlineData("International", 5, 1500)]
    [InlineData("Freight", 5, 250)]
    public async Task GetRate_WithValidType_ShouldReturnOk(
        string type,
        double weight,
        decimal expectedRate)
    {
        _service
            .Setup(x => x.CalculateRateAsync(
                weight,
                It.IsAny<ShipmentType>()))
            .ReturnsAsync(expectedRate);

        var result =
            await _controller.GetRate(
                weight,
                type);

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);

        var value = okResult.Value;

        Assert.NotNull(value);
    }

    [Fact]
    public async Task GetRate_WithInvalidType_ShouldReturnBadRequest()
    {
        var result =
            await _controller.GetRate(
                5,
                "InvalidShipmentType");

        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "Invalid type",
            badRequest.Value);

        _service.Verify(
            x => x.CalculateRateAsync(
                It.IsAny<double>(),
                It.IsAny<ShipmentType>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelShipment_WithValidUser_ShouldReturnOk()
    {
        SetUser(100);

        var request = new CancelShipmentRequest
        {
            Reason = "Changed my mind"
        };

        _service
            .Setup(x => x.CancelByCustomerAsync(
                1,
                100,
                "Changed my mind"))
            .Returns(Task.CompletedTask);

        var result =
            await _controller.CancelShipment(
                1,
                request);

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);

        _service.Verify(
            x => x.CancelByCustomerAsync(
                1,
                100,
                "Changed my mind"),
            Times.Once);
    }

    [Fact]
    public async Task CancelShipment_WhenUserIdMissing_ShouldReturnUnauthorized()
    {
        _controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

        var request = new CancelShipmentRequest
        {
            Reason = "Changed my mind"
        };

        var result =
            await _controller.CancelShipment(
                1,
                request);

        var unauthorized =
            Assert.IsType<UnauthorizedObjectResult>(result);

        Assert.Equal(401, unauthorized.StatusCode);

        _service.Verify(
            x => x.CancelByCustomerAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByTrackingNumber_WhenShipmentExists_ShouldReturnOk()
    {
        SetUser(100);

        var response = CreateResponse();

        _service
            .Setup(x => x.GetByTrackingNumberAsync(
                "SHP-20260815-1234"))
            .ReturnsAsync(response);

        var result =
            await _controller.GetByTrackingNumber(
                "SHP-20260815-1234");

        var okResult =
            Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetByTrackingNumber_WhenShipmentDoesNotExist_ShouldReturnNotFound()
    {
        SetUser(100);

        _service
            .Setup(x => x.GetByTrackingNumberAsync(
                "INVALID"))
            .ReturnsAsync((ShipmentResponse?)null);

        var result =
            await _controller.GetByTrackingNumber(
                "INVALID");

        var notFound =
            Assert.IsType<NotFoundResult>(result);

        Assert.Equal(404, notFound.StatusCode);
    }
}