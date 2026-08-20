using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Persistence;
using SmartShip.Identity.Application.Interfaces.Repositories;
using SmartShip.Identity.Application.Services;
using SmartShip.Identity.Domain.Entities;
using Xunit;

namespace SmartShip.Identity.Tests.Services;

/// <summary>
/// Unit test suite for verifying <see cref="AuthService"/> authentication, registration, BCrypt password hashing, and JWT token issuance logic.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publisherMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _publisherMock = new Mock<IPublishEndpoint>();

        var configurationData = new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "SmartShipTestSecretKey2026SuperSecureKey123456789!",
            ["JwtSettings:Issuer"] = "SmartShipTestIssuer",
            ["JwtSettings:Audience"] = "SmartShipTestAudience",
            ["JwtSettings:ExpiryMinutes"] = "60",
            ["AdminSettings:DefaultPassword"] = "Admin@123"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SignupAsync_NewUser_ShouldCreateCustomerAndReturnAuthResponse()
    {
        var request = new SignupRequest(
            "John Doe",
            "john@example.com",
            "9876543210",
            "Password@123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _authService.SignupAsync(request);

        Assert.NotNull(result);
        Assert.Equal("CUSTOMER", result.Role);
        Assert.Equal("John Doe", result.Name);

        _userRepositoryMock.Verify(
            x => x.GetByEmailAsync(request.Email),
            Times.Once);

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.Is<User>(u =>
                u.Name == "John Doe" &&
                u.Email == "john@example.com" &&
                u.Phone == "9876543210" &&
                u.Role == "CUSTOMER" &&
                u.IsActive)),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SignupAsync_ExistingEmail_ShouldThrowInvalidOperationException()
    {
        var request = new SignupRequest(
            "John Doe",
            "john@example.com",
            "9876543210",
            "Password@123");

        var existingUser = new User
        {
            Id = 1,
            Name = "Existing User",
            Email = "john@example.com",
            Phone = "9999999999",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
            Role = "CUSTOMER",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.SignupAsync(request));

        Assert.Equal(
            "User with this email already exists.",
            exception.Message);

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SignupAsync_ShouldHashPassword()
    {
        var plainPassword = "Password@123";

        var request = new SignupRequest(
            "John Doe",
            "john@example.com",
            "9876543210",
            plainPassword);

        User? createdUser = null;

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => createdUser = user)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _authService.SignupAsync(request);

        Assert.NotNull(createdUser);
        Assert.NotEmpty(createdUser!.PasswordHash);
        Assert.NotEqual(plainPassword, createdUser.PasswordHash);

        Assert.True(
            BCrypt.Net.BCrypt.Verify(
                plainPassword,
                createdUser.PasswordHash));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        var password = "Password@123";

        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "CUSTOMER",
            IsActive = true
        };

        var request = new LoginRequest(
            "john@example.com",
            password);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var result = await _authService.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Role, result.Role);
        Assert.NotEmpty(result.Token);

        _userRepositoryMock.Verify(
            x => x.GetByEmailAsync(request.Email),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var request = new LoginRequest(
            "missing@example.com",
            "Password@123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _authService.LoginAsync(request));

        Assert.Equal(
            "User not found with this email. Please signup.",
            exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ShouldThrowUnauthorizedAccessException()
    {
        var user = new User
        {
            Id = 1,
            Name = "Inactive User",
            Email = "inactive@example.com",
            Phone = "9876543210",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
            Role = "CUSTOMER",
            IsActive = false
        };

        var request = new LoginRequest(
            "inactive@example.com",
            "Password@123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));

        Assert.Equal(
            "User account is inactive.",
            exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123"),
            Role = "CUSTOMER",
            IsActive = true
        };

        var request = new LoginRequest(
            "john@example.com",
            "WrongPassword@123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));

        Assert.Equal(
            "Incorrect password.",
            exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldGenerateJwtContainingUserInformation()
    {
        var password = "Password@123";

        var user = new User
        {
            Id = 15,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "CUSTOMER",
            IsActive = true
        };

        var request = new LoginRequest(
            "john@example.com",
            password);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var result = await _authService.LoginAsync(request);

        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.Contains(".", result.Token);
    }

    [Fact]
    public async Task DebugLogin_UserDoesNotExist_ShouldReturnFailed()
    {
        var request = new LoginRequest(
            "missing@example.com",
            "Password@123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        var result = await _authService.DebugLoginAsync(request);

        Assert.NotNull(result);

        var resultType = result.GetType();

        Assert.Equal(
            "FAILED",
            resultType.GetProperty("step")!.GetValue(result));

        Assert.Equal(
            "User not found",
            resultType.GetProperty("reason")!.GetValue(result));

        Assert.Equal(
            request.Email,
            resultType.GetProperty("email")!.GetValue(result));
    }

    [Fact]
    public async Task DebugLogin_CorrectPassword_ShouldReturnSuccess()
    {
        var password = "Password@123";

        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "CUSTOMER",
            IsActive = true
        };

        var request = new LoginRequest(
            user.Email,
            password);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var result = await _authService.DebugLoginAsync(request);

        var resultType = result.GetType();

        Assert.Equal(
            "SUCCESS",
            resultType.GetProperty("step")!.GetValue(result));

        Assert.Equal(
            "Password matches",
            resultType.GetProperty("reason")!.GetValue(result));
    }

    [Fact]
    public async Task DebugLogin_WrongPassword_ShouldReturnFailed()
    {
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123"),
            Role = "CUSTOMER",
            IsActive = true
        };

        var request = new LoginRequest(
            user.Email,
            "WrongPassword@123");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var result = await _authService.DebugLoginAsync(request);

        var resultType = result.GetType();

        Assert.Equal(
            "FAILED",
            resultType.GetProperty("step")!.GetValue(result));

        Assert.Equal(
            "Wrong password",
            resultType.GetProperty("reason")!.GetValue(result));
    }
}