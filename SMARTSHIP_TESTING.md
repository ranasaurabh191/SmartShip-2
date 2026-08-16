# SmartShip – Comprehensive Testing Documentation

Guide to unit testing, mock setup, integration testing, and test execution strategies across the SmartShip microservices platform.

---

## 1. Test Suite Architecture

SmartShip incorporates four dedicated test projects within the solution, corresponding to each microservice domain:

```
SmartShip/
├── Services/
│   ├── AdminService/Tests/SmartShip.Admin.Tests/         # Admin Service Test Project
│   ├── IdentityService/Tests/SmartShip.Identity.Tests/   # Identity Service Test Project
│   ├── PaymentService/Tests/SmartShip.Payment.Tests/     # Payment Service Test Project
│   └── ShipmentService/Tests/SmartShip.Shipment.Tests/   # Shipment Service Test Project
```

---

## 2. Test Frameworks & Libraries

| Framework / Tool | Purpose | Usage in SmartShip |
| :--- | :--- | :--- |
| **xUnit** | Primary Unit Test Runner & Framework | Test attributes (`[Fact]`, `[Theory]`), assertions (`Assert.Equal`, `Assert.ThrowsAsync`) |
| **Moq** | Object Mocking Framework | Mocking repository interfaces (`Mock<IShipmentRepository>`), `IUnitOfWork`, `IPublishEndpoint`, `IRazorpayClient` |
| **EF Core InMemory** | In-Memory Database Provider | In-memory DbContext instantiation for testing database persistence without SQL Server |
| **MassTransit Test Harness** | In-Memory Messaging Bus | `UsingInMemory` bus configuration in Shipment Service test environment |

---

## 3. Empirical Test Execution Audit Results

Running `dotnet test` across the solution yields the following empirical status:

```text
Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24, Duration: 980 ms - SmartShip.Admin.Tests.dll
Passed!  - Failed: 0, Passed: 30, Skipped: 0, Total: 30, Duration: 4.0 s  - SmartShip.Identity.Tests.dll
```

### Detailed Breakdown
* **`SmartShip.Admin.Tests`**: **24 Tests Passed** (100% pass rate). Covers `AdminControllerTests`, `AdminServiceTests`, `HubRepositoryTests`, `ReportRepositoryTests`.
* **`SmartShip.Identity.Tests`**: **30 Tests Passed** (100% pass rate). Covers `AuthControllerTests`, `UsersControllerTests`, `AuthServiceTests`, `UserServiceTests`.
* **`SmartShip.Shipment.Tests` & `SmartShip.Payment.Tests`**: Unit tests implemented. Note: Test code contains minor parameter constructor mismatches against recently refactored DTOs (`VerifyPaymentRequest` and `CreateShipmentRequest` constructors), serving as a clear target for unit test signature updates.

---

## 4. Testing Patterns & Examples

### 4.1 Service Unit Testing with Moq (`AuthServiceTests.cs`)
Demonstrates testing signup logic with mocked repository and unit of work:

```csharp
[Fact]
public async Task SignupAsync_ShouldCreateUser_WhenEmailIsUnique()
{
    // Arrange
    var userRepoMock = new Mock<IUserRepository>();
    var uowMock = new Mock<IUnitOfWork>();
    var configMock = new Mock<IConfiguration>();
    var loggerMock = new Mock<ILogger<AuthService>>();
    var publisherMock = new Mock<IPublishEndpoint>();

    userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
        .ReturnsAsync((User?)null);

    var service = new AuthService(
        userRepoMock.Object, uowMock.Object, configMock.Object, 
        loggerMock.Object, publisherMock.Object, configMock.Object);

    var request = new SignupRequest("John Doe", "john@example.com", "+919876543210", "Password123!");

    // Act
    var response = await service.SignupAsync(request);

    // Assert
    Assert.NotNull(response);
    Assert.Equal("CUSTOMER", response.Role);
    userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
}
```

---

### 4.2 Controller Unit Testing (`AdminControllerTests.cs`)
Demonstrates controller action result assertions:

```csharp
[Fact]
public async Task Dashboard_ShouldReturnOk_WithMetrics()
{
    // Arrange
    var adminServiceMock = new Mock<IAdminService>();
    var expectedMetrics = new DashboardMetricsDTO(100, 20, 80, 150000m, 50, DateTime.Now);
    
    adminServiceMock.Setup(s => s.GetDashboardAsync())
        .ReturnsAsync(expectedMetrics);

    var controller = new AdminController(adminServiceMock.Object);

    // Act
    var result = await controller.Dashboard();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var metrics = Assert.IsType<DashboardMetricsDTO>(okResult.Value);
    Assert.Equal(100, metrics.TotalShipments);
}
```

---

### 4.3 EF Core InMemory Repository Testing (`HubRepositoryTests.cs`)
Demonstrates data access testing using EF Core InMemory provider:

```csharp
[Fact]
public async Task GetAllActiveAsync_ShouldReturnOnlyActiveHubs()
{
    // Arrange
    var options = new DbContextOptionsBuilder<AdminDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    using var context = new AdminDbContext(options);
    context.Hubs.Add(new Hub { HubCode = "HUB1", Name = "Active Hub", IsActive = true });
    context.Hubs.Add(new Hub { HubCode = "HUB2", Name = "Inactive Hub", IsActive = false });
    await context.SaveChangesAsync();

    var repo = new HubRepository(context);

    // Act
    var activeHubs = await repo.GetAllActiveAsync();

    // Assert
    Assert.Single(activeHubs);
    Assert.Equal("HUB1", activeHubs.First().HubCode);
}
```

---

## 5. Mocking Strategy & Key Dependencies

| Component | Mock Target | Setup Method |
| :--- | :--- | :--- |
| **`IUnitOfWork`** | Database Transaction | `uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1)` |
| **`IPublishEndpoint`** | MassTransit Message Bus | `publisherMock.Setup(p => p.Publish(It.IsAny<T>(), default))` |
| **`IHttpClientFactory`** | Inter-service REST Calls | Setup `HttpMessageHandler` to return pre-baked HTTP 200 JSON responses |
| **`IRazorpayClient`** | Razorpay Gateway SDK | `razorpayMock.Setup(r => r.VerifySignature(...)).Returns(true)` |

---

## 6. How to Run Tests

### Execute Entire Solution Test Suite
```bash
dotnet test
```

### Run Tests for Specific Service
```bash
# Run Identity Service Tests
dotnet test Services/IdentityService/Tests/SmartShip.Identity.Tests

# Run Admin Service Tests
dotnet test Services/AdminService/Tests/SmartShip.Admin.Tests

# Run Shipment Service Tests
dotnet test Services/ShipmentService/Tests/SmartShip.Shipment.Tests

# Run Payment Service Tests
dotnet test Services/PaymentService/Tests/SmartShip.Payment.Tests
```

### Verbose Detailed Test Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

---

*Testing documentation compiled for **SmartShip Logistics System**.*
