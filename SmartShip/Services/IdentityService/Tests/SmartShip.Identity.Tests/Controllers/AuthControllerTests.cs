using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartShip.Identity.API.Controllers;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Services;
using Xunit;

namespace SmartShip.Identity.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_ValidRequest_ShouldReturnOk()
    {
        var request = new LoginRequest
        (
           "john@example.com",
           "Password@123"
        );

        var expectedResponse = new AuthResponse(
            "fake-jwt-token",
            "CUSTOMER",
            "John Doe",
            1);

        _authServiceMock
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(expectedResponse, okResult.Value);

        _authServiceMock.Verify(
            x => x.LoginAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task Signup_ValidRequest_ShouldReturnOk()
    {
        var request = new SignupRequest
        (
            "John Doe",
            "john@example.com",
            "9876543210",
            "Password@123"
        );

        var expectedResponse = new AuthResponse(
            "fake-jwt-token",
            "CUSTOMER",
            "John Doe",
            1);

        _authServiceMock
            .Setup(x => x.SignupAsync(request))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.Signup(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(expectedResponse, okResult.Value);

        _authServiceMock.Verify(
            x => x.SignupAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task DebugLogin_ValidRequest_ShouldReturnOk()
    {
        var request = new LoginRequest
        (
            "admin@smartship.com",
            "Admin@123"
        );

        var expectedResponse = new
        {
            step = "SUCCESS",
            reason = "Password matches",
            email = "admin@smartship.com",
            role = "ADMIN",
            isActive = true
        };

        _authServiceMock
            .Setup(x => x.DebugLoginAsync(request))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.DebugLogin(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(expectedResponse, okResult.Value);

        _authServiceMock.Verify(
            x => x.DebugLoginAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task FixAdmin_ShouldReturnOk()
    {
        var expectedResponse = new
        {
            message = "Admin fixed successfully."
        };

        _authServiceMock
            .Setup(x => x.FixAdminAsync())
            .ReturnsAsync(expectedResponse);

        var result = await _controller.FixAdmin();

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(expectedResponse, okResult.Value);

        _authServiceMock.Verify(
            x => x.FixAdminAsync(),
            Times.Once);
    }

    [Fact]
    public async Task Login_ServiceThrowsException_ShouldPropagateException()
    {
        var request = new LoginRequest
        (
            "missing@example.com",
            "Password@123"
        );

        _authServiceMock
            .Setup(x => x.LoginAsync(request))
            .ThrowsAsync(
                new KeyNotFoundException(
                    "User not found with this email. Please signup."));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.Login(request));

        Assert.Equal(
            "User not found with this email. Please signup.",
            exception.Message);
    }
}