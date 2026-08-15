using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Admin.Application.DTOs;
using SmartShip.Admin.Application.Services;
using SmartShip.Admin.Domain.Entities;
using SmartShip.Admin.Domain.Enums;
using System.Security.Claims;

namespace SmartShip.Admin.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<IHubRepository> _hubRepository;
    private readonly Mock<IReportRepository> _reportRepository;
    private readonly Mock<IDashboardMetricsRepository> _dashboardRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<AdminService>> _logger;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

    public AdminServiceTests()
    {
        _hubRepository = new Mock<IHubRepository>();
        _reportRepository = new Mock<IReportRepository>();
        _dashboardRepository = new Mock<IDashboardMetricsRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<AdminService>>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private AdminService CreateService()
    {
        return new AdminService(
            _hubRepository.Object,
            _reportRepository.Object,
            _dashboardRepository.Object,
            _unitOfWork.Object,
            _logger.Object,
            _httpContextAccessor.Object);
    }

    private void SetAuthenticatedAdmin(
        string name = "Admin User")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, "admin@smartship.com"),
            new Claim(ClaimTypes.Role, "ADMIN")
        };

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication");

        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(context);
    }

    private void SetAuthenticatedCustomer()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Customer User"),
            new Claim(ClaimTypes.Email, "customer@example.com"),
            new Claim(ClaimTypes.Role, "CUSTOMER")
        };

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication");

        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(context);
    }

    private void SetUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity();

        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(context);
    }

    private static Hub CreateHub(
        int id = 1,
        string name = "Delhi Hub",
        string city = "Delhi",
        string state = "Delhi",
        string country = "India",
        string phone = "9876543210",
        bool isActive = true)
    {
        return new Hub
        {
            Id = id,
            Name = name,
            City = city,
            State = state,
            Country = country,
            ContactPhone = phone,
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };
    }

    private static DashboardMetrics CreateMetrics()
    {
        return new DashboardMetrics
        {
            Id = 1,
            TotalShipments = 100,
            ActiveShipments = 30,
            DeliveredToday = 15,
            TotalCustomers = 50,
            LastUpdatedAt = DateTime.Now
        };
    }

    [Fact]
    public async Task GetDashboardAsync_UnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
    {
        SetUnauthenticatedUser();

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetDashboardAsync());

        _dashboardRepository.Verify(
            x => x.GetFirstAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetDashboardAsync_NonAdminUser_ShouldThrowUnauthorizedAccessException()
    {
        SetAuthenticatedCustomer();

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetDashboardAsync());

        _dashboardRepository.Verify(
            x => x.GetFirstAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetDashboardAsync_AdminWithExistingMetrics_ShouldReturnMetrics()
    {
        SetAuthenticatedAdmin();

        var metrics = CreateMetrics();

        _dashboardRepository
            .Setup(x => x.GetFirstAsync())
            .ReturnsAsync(metrics);

        var service = CreateService();

        var result = await service.GetDashboardAsync();

        Assert.NotNull(result);
        Assert.Equal(100, result.TotalShipments);
        Assert.Equal(30, result.ActiveShipments);
        Assert.Equal(15, result.DeliveredToday);
        Assert.Equal(50, result.TotalCustomers);
        Assert.NotNull(result.LastUpdatedAt);

        _dashboardRepository.Verify(
            x => x.GetFirstAsync(),
            Times.Once);

        _dashboardRepository.Verify(
            x => x.UpdateAsync(metrics),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenMetricsDoNotExist_ShouldCreateDefaultMetrics()
    {
        SetAuthenticatedAdmin();

        _dashboardRepository
            .Setup(x => x.GetFirstAsync())
            .ReturnsAsync((DashboardMetrics?)null);

        DashboardMetrics? createdMetrics = null;

        _dashboardRepository
            .Setup(x => x.AddAsync(It.IsAny<DashboardMetrics>()))
            .Callback<DashboardMetrics>(metrics =>
            {
                createdMetrics = metrics;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.GetDashboardAsync();

        Assert.NotNull(createdMetrics);
        Assert.Equal(0, createdMetrics!.TotalShipments);
        Assert.Equal(0, createdMetrics.ActiveShipments);
        Assert.Equal(0, createdMetrics.DeliveredToday);
        Assert.Equal(0, createdMetrics.TotalCustomers);
        Assert.NotNull(createdMetrics.LastUpdatedAt);

        Assert.Equal(0, result.TotalShipments);
        Assert.Equal(0, result.ActiveShipments);
        Assert.Equal(0, result.DeliveredToday);
        Assert.Equal(0, result.TotalCustomers);

        _dashboardRepository.Verify(
            x => x.AddAsync(It.IsAny<DashboardMetrics>()),
            Times.Once);

        _dashboardRepository.Verify(
            x => x.UpdateAsync(It.IsAny<DashboardMetrics>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetHubByIdAsync_WhenHubExists_ShouldReturnHub()
    {
        SetAuthenticatedAdmin();

        var hub = CreateHub(
            101,
            "Bangalore Hub",
            "Bengaluru",
            "Karnataka",
            "India",
            "9800000003");

        _hubRepository
            .Setup(x => x.GetByIdAsync(101))
            .ReturnsAsync(hub);

        var service = CreateService();

        var result = await service.GetHubByIdAsync(101);

        Assert.NotNull(result);
        Assert.Equal(101, result.Id);
        Assert.Equal("Bangalore Hub", result.Name);
        Assert.Equal("Bengaluru", result.City);
        Assert.Equal("Karnataka", result.State);
        Assert.Equal("India", result.Country);
        Assert.Equal("9800000003", result.ContactPhone);
        Assert.True(result.IsActive);

        _hubRepository.Verify(
            x => x.GetByIdAsync(101),
            Times.Once);
    }

    [Fact]
    public async Task GetHubByIdAsync_WhenHubDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        SetAuthenticatedAdmin();

        _hubRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Hub?)null);

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetHubByIdAsync(999));

        Assert.Equal(
            "Hub 999 not found.",
            exception.Message);
    }

    [Fact]
    public async Task GetHubByIdAsync_NonAdminUser_ShouldThrowUnauthorizedAccessException()
    {
        SetAuthenticatedCustomer();

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetHubByIdAsync(101));
    }

    [Fact]
    public async Task CreateHubAsync_ShouldCreateHubAndReturnDto()
    {
        SetAuthenticatedAdmin();

        var request = new CreateHubRequest(
            "Delhi Hub",
            "Delhi",
            "Delhi",
            "India",
            "9876543210");

        _hubRepository
            .Setup(x => x.AddAsync(It.IsAny<Hub>()))
            .ReturnsAsync((Hub hub) =>
            {
                hub.Id = 101;
                return hub;
            });

        var service = CreateService();

        var result = await service.CreateHubAsync(request);

        Assert.NotNull(result);
        Assert.Equal(101, result.Id);
        Assert.Equal("Delhi Hub", result.Name);
        Assert.Equal("Delhi", result.City);
        Assert.Equal("Delhi", result.State);
        Assert.Equal("India", result.Country);
        Assert.Equal("9876543210", result.ContactPhone);
        Assert.True(result.IsActive);

        _hubRepository.Verify(
            x => x.AddAsync(It.Is<Hub>(h =>
                h.Name == "Delhi Hub" &&
                h.City == "Delhi" &&
                h.State == "Delhi" &&
                h.Country == "India" &&
                h.ContactPhone == "9876543210")),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateHubAsync_NonAdminUser_ShouldThrowUnauthorizedAccessException()
    {
        SetAuthenticatedCustomer();

        var request = new CreateHubRequest(
            "Delhi Hub",
            "Delhi",
            "Delhi",
            "India",
            "9876543210");

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateHubAsync(request));

        _hubRepository.Verify(
            x => x.AddAsync(It.IsAny<Hub>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateHubAsync_WhenHubExists_ShouldUpdateHub()
    {
        SetAuthenticatedAdmin();

        var hub = CreateHub(
            101,
            "Old Hub",
            "Old City",
            "Old State",
            "India",
            "9000000000",
            true);

        _hubRepository
            .Setup(x => x.GetByIdAsync(101))
            .ReturnsAsync(hub);

        _hubRepository
            .Setup(x => x.UpdateAsync(hub))
            .Returns(Task.CompletedTask);

        var request = new UpdateHubRequest(
            "New Hub",
            "New City",
            "New State",
            "India",
            "9876543210",
            false);

        var service = CreateService();

        await service.UpdateHubAsync(101, request);

        Assert.Equal("New Hub", hub.Name);
        Assert.Equal("New City", hub.City);
        Assert.Equal("New State", hub.State);
        Assert.Equal("India", hub.Country);
        Assert.Equal("9876543210", hub.ContactPhone);
        Assert.False(hub.IsActive);

        _hubRepository.Verify(
            x => x.UpdateAsync(hub),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHubAsync_WhenHubDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        SetAuthenticatedAdmin();

        _hubRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Hub?)null);

        var request = new UpdateHubRequest(
            "New Hub",
            "New City",
            "New State",
            "India",
            "9876543210",
            true);

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateHubAsync(999, request));

        Assert.Equal(
            "Hub 999 not found.",
            exception.Message);

        _hubRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Hub>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteHubAsync_WhenHubExists_ShouldDeleteHub()
    {
        SetAuthenticatedAdmin();

        var hub = CreateHub(
            101,
            "Delhi Hub",
            "Delhi",
            "Delhi",
            "India",
            "9876543210");

        _hubRepository
            .Setup(x => x.GetByIdAsync(101))
            .ReturnsAsync(hub);

        _hubRepository
            .Setup(x => x.DeleteAsync(hub))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.DeleteHubAsync(101);

        _hubRepository.Verify(
            x => x.GetByIdAsync(101),
            Times.Once);

        _hubRepository.Verify(
            x => x.DeleteAsync(hub),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteHubAsync_WhenHubDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        SetAuthenticatedAdmin();

        _hubRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Hub?)null);

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteHubAsync(999));

        Assert.Equal(
            "Hub 999 not found.",
            exception.Message);

        _hubRepository.Verify(
            x => x.DeleteAsync(It.IsAny<Hub>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllActiveHubsAsync_ShouldReturnMappedHubDtos()
    {
        var hubs = new List<Hub>
        {
            CreateHub(
                101,
                "Bangalore Hub",
                "Bengaluru",
                "Karnataka",
                "India",
                "9800000003",
                true),

            CreateHub(
                102,
                "Hyderabad Hub",
                "Hyderabad",
                "Telangana",
                "India",
                "9800000004",
                true)
        };

        _hubRepository
            .Setup(x => x.GetAllActiveAsync())
            .ReturnsAsync(hubs);

        var service = CreateService();

        var result = await service.GetAllActiveHubsAsync();

        var resultList = result.ToList();

        Assert.Equal(2, resultList.Count);

        Assert.Equal(101, resultList[0].Id);
        Assert.Equal("Bangalore Hub", resultList[0].Name);
        Assert.Equal("Bengaluru", resultList[0].City);

        Assert.Equal(102, resultList[1].Id);
        Assert.Equal("Hyderabad Hub", resultList[1].Name);
        Assert.Equal("Hyderabad", resultList[1].City);

        _hubRepository.Verify(
            x => x.GetAllActiveAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllActiveHubsAsync_WhenNoHubs_ShouldReturnEmptyCollection()
    {
        _hubRepository
            .Setup(x => x.GetAllActiveAsync())
            .ReturnsAsync(new List<Hub>());

        var service = CreateService();

        var result = await service.GetAllActiveHubsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldCreateReport()
    {
        SetAuthenticatedAdmin("Saurabh Rana");

        var metrics = new DashboardMetrics
        {
            Id = 1,
            TotalShipments = 100,
            ActiveShipments = 30,
            DeliveredToday = 20,
            TotalCustomers = 50,
            LastUpdatedAt = DateTime.Now
        };

        _dashboardRepository
            .Setup(x => x.GetFirstAsync())
            .ReturnsAsync(metrics);

        _reportRepository
            .Setup(x => x.AddAsync(It.IsAny<Report>()))
            .ReturnsAsync((Report report) =>
            {
                report.Id = 10;
                return report;
            });

        var fromDate = new DateTime(2026, 8, 1);
        var toDate = new DateTime(2026, 8, 15);

        var request = new ReportRequest(
            "Operational",
            fromDate,
            toDate);

        var service = CreateService();

        var result = await service.GenerateReportAsync(request);

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Contains("Operational Report", result.Title);
        Assert.Equal("Operational", result.ReportType);
        Assert.Equal(fromDate, result.FromDate);
        Assert.Equal(toDate, result.ToDate);
        Assert.NotNull(result.Data);

        _reportRepository.Verify(
            x => x.AddAsync(It.Is<Report>(r =>
                r.GeneratedBy == "Saurabh Rana" &&
                r.ReportType == ReportType.Operational &&
                r.FromDate == fromDate &&
                r.ToDate == toDate)),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task GenerateReportAsync_ShouldCalculateDeliveredShipments()
    {
        SetAuthenticatedAdmin("Admin User");

        var metrics = new DashboardMetrics
        {
            TotalShipments = 100,
            ActiveShipments = 25,
            DeliveredToday = 10,
            TotalCustomers = 50,
            LastUpdatedAt = DateTime.Now
        };

        _dashboardRepository
            .Setup(x => x.GetFirstAsync())
            .ReturnsAsync(metrics);

        Report? savedReport = null;

        _reportRepository
            .Setup(x => x.AddAsync(It.IsAny<Report>()))
            .Callback<Report>(report =>
            {
                savedReport = report;
                report.Id = 1;
            })
            .ReturnsAsync((Report report) => report);

        var request = new ReportRequest(
            "SHIPMENT",
            new DateTime(2026, 8, 1),
            new DateTime(2026, 8, 15));

        var service = CreateService();

        await service.GenerateReportAsync(request);

        Assert.NotNull(savedReport);
        Assert.Contains("\"TotalShipments\":100", savedReport!.DataJson);
        Assert.Contains("\"Delivered\":75", savedReport.DataJson);
        Assert.Contains("\"ActiveShipments\":25", savedReport.DataJson);
    }

}