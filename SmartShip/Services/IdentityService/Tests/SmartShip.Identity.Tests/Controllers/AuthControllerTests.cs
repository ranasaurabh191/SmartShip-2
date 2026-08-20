using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartShip.Identity.API.Controllers;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Services;
using System.Security.Claims;
using Xunit;

namespace SmartShip.Identity.Tests.Controllers;

/// <summary>
/// Unit test suite for verifying <see cref="AuthController"/> endpoint behavior, claim processing, and HTTP status code mappings.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    /// <summary>
    /// Verifies that <see cref="AuthController.Login"/> returns HTTP 200 (OK) with valid credentials.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="AuthController.Signup"/> returns HTTP 200 (OK) with valid registration data.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="AuthController.DebugLogin"/> returns HTTP 200 (OK) with diagnostic info.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="AuthController.Login"/> rethrows exception when service fails.
    /// </summary>
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

    /// <summary>
    /// Verifies profile update with a valid JWT user claim.
    /// </summary>
    [Fact]
    public async Task UpdateProfile_ValidUser_ShouldReturnOk()
    {
        var request = new UpdateMyProfileRequest(
            "Updated User",
            "updated@example.com",
            "9876543210");
        var userDto = new UserDto(
            1,
            "Updated User",
            "updated@example.com",
            "9876543210",
            "CUSTOMER",
            true,
            DateTime.Now);

        _authServiceMock
            .Setup(x => x.UpdateMyProfileAsync(1, request)).ReturnsAsync(userDto);

        var claims = new[]{new Claim(ClaimTypes.NameIdentifier, "1")};

        var identity = new ClaimsIdentity(claims, "TestAuthentication");

        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        var result = await _controller.UpdateProfile(request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(okResult.Value);

        _authServiceMock.Verify(
            x => x.UpdateMyProfileAsync(1, request),
            Times.Once);
    }

    /// <summary>
    /// Verifies profile update returns HTTP 401 (Unauthorized) when user ID claim is missing.
    /// </summary>
    [Fact]
    public async Task UpdateProfile_MissingUserIdClaim_ShouldReturnUnauthorized()
    {
        var request = new UpdateMyProfileRequest(
            "Updated User",
            "updated@example.com",
            "9876543210");

        var identity = new ClaimsIdentity(
            Array.Empty<Claim>(),
            "TestAuthentication");

        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        var result = await _controller.UpdateProfile(request);

        var unauthorizedResult =
            Assert.IsType<UnauthorizedObjectResult>(result);

        Assert.NotNull(unauthorizedResult.Value);

        _authServiceMock.Verify(
            x => x.UpdateMyProfileAsync(
                It.IsAny<int>(),
                It.IsAny<UpdateMyProfileRequest>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies profile update returns HTTP 401 (Unauthorized) when user ID claim cannot be parsed as integer.
    /// </summary>
    [Fact]
    public async Task UpdateProfile_InvalidUserIdClaim_ShouldReturnUnauthorized()
    {
        var request = new UpdateMyProfileRequest(
            "Updated User",
            "updated@example.com",
            "9876543210");

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, "invalid-id")
    };

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication");

        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        var result = await _controller.UpdateProfile(request);

        Assert.IsType<UnauthorizedObjectResult>(result);

        _authServiceMock.Verify(
            x => x.UpdateMyProfileAsync(
                It.IsAny<int>(),
                It.IsAny<UpdateMyProfileRequest>()),
            Times.Never);
    }
}