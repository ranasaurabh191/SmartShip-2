using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartShip.Admin.API.Controllers;
using SmartShip.Admin.Application.DTOs;
using System.Security.Claims; 

namespace SmartShip.Admin.Tests.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IAdminService> _adminService;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _adminService = new Mock<IAdminService>();
        _controller = new AdminController(_adminService.Object);
    }

    private void SetAdminUser()
    {
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, "Admin User"),
        new Claim(ClaimTypes.Role, "ADMIN")
    };

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    } 

    [Fact]
    public async Task Dashboard_ShouldReturnOk()
    {
        var response = new DashboardMetricsDTO
        {
            TotalShipments = 100,
            ActiveShipments = 30,
            DeliveredToday = 15,
            TotalCustomers = 50,
            LastUpdatedAt = "15-Aug-2026 11:30 AM"
        };

        _adminService
            .Setup(x => x.GetDashboardAsync())
            .ReturnsAsync(response);

        var result = await _controller.Dashboard();

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(response, okResult.Value);

        _adminService.Verify(
            x => x.GetDashboardAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllActiveHubs_ShouldReturnOk()
    {
        var hubs = new List<HubDTO>
        {
            new(
                101,
                "Bangalore Hub",
                "Bengaluru",
                "Karnataka",
                "India",
                "9800000003",
                true),

            new(
                102,
                "Hyderabad Hub",
                "Hyderabad",
                "Telangana",
                "India",
                "9800000004",
                true)
        };

        _adminService
            .Setup(x => x.GetAllActiveHubsAsync())
            .ReturnsAsync(hubs);

        var result = await _controller.GetAllActiveHubs();

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(hubs, okResult.Value);

        _adminService.Verify(
            x => x.GetAllActiveHubsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetHub_WhenHubExists_ShouldReturnOk()
    {
        var hub = new HubDTO(
            101,
            "Bangalore Hub",
            "Bengaluru",
            "Karnataka",
            "India",
            "9800000003",
            true);

        _adminService
            .Setup(x => x.GetHubByIdAsync(101))
            .ReturnsAsync(hub);

        var result = await _controller.GetHub(101);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(hub, okResult.Value);

        _adminService.Verify(
            x => x.GetHubByIdAsync(101),
            Times.Once);
    }

    
    [Fact]
    public async Task CreateHub_ShouldReturnOk()
    {
        var request = new CreateHubRequest(
            "Delhi Hub",
            "Delhi",
            "Delhi",
            "India",
            "9876543210");

        var response = new HubDTO(
            101,
            "Delhi Hub",
            "Delhi",
            "Delhi",
            "India",
            "9876543210",
            true);

        _adminService
            .Setup(x => x.CreateHubAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.CreateHub(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(response, okResult.Value);

        _adminService.Verify(
            x => x.CreateHubAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHub_ShouldReturnOk()
    {
        var request = new UpdateHubRequest(
            "Updated Hub",
            "Delhi",
            "Delhi",
            "India",
            "9999999999",
            true);

        _adminService
            .Setup(x => x.UpdateHubAsync(101, request))
            .Returns(Task.CompletedTask);

        var result = await _controller.UpdateHub(101, request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(
            "Updated Successfully",
            okResult.Value);

        _adminService.Verify(
            x => x.UpdateHubAsync(101, request),
            Times.Once);
    }

    [Fact]
    public async Task DeleteHub_ShouldReturnOk()
    {
        _adminService
            .Setup(x => x.DeleteHubAsync(101))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteHub(101);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);

        Assert.NotNull(okResult.Value);

        var messageProperty = okResult.Value!
            .GetType()
            .GetProperty("message");

        Assert.NotNull(messageProperty);

        Assert.Equal(
            "Deleted Successfully",
            messageProperty!.GetValue(okResult.Value));

        _adminService.Verify(
            x => x.DeleteHubAsync(101),
            Times.Once);
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnOk()
    {
        SetAdminUser();

        var request = new ReportRequest(
            "SHIPMENT",
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 15));

        var response = new ReportDTO(
            1,
            "SHIPMENT Report (8/1/2026 - 8/15/2026)",
            "SHIPMENT",
            request.FromDate,
            request.ToDate,
            DateTime.Now,
            new
            {
                TotalShipments = 100,
                Delivered = 70,
                ActiveShipments = 30,
                GeneratedFrom = request.FromDate,
                GeneratedTo = request.ToDate
            });

        _adminService
            .Setup(x => x.GenerateReportAsync(request))
            .ReturnsAsync(response);

        var result = await _controller.GenerateReport(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(response, okResult.Value);

        _adminService.Verify(
            x => x.GenerateReportAsync(request),
            Times.Once);
    }
}