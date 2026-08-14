using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartShip.Identity.API.Controllers;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Services;
using Xunit;

namespace SmartShip.Identity.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
    }

    [Fact]
    public async Task GetById_UserExists_ShouldReturnOk()
    {
        var userId = 1;

        var expectedUser = new UserDto(
            1,
            "John Doe",
            "john@example.com",
            "9876543210",
            "CUSTOMER",
            true,
            DateTime.UtcNow);

        _userServiceMock
            .Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        var result = await _controller.GetById(userId);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);
        Assert.Same(expectedUser, okResult.Value);

        _userServiceMock.Verify(
            x => x.GetUserByIdAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task GetById_UserDoesNotExist_ShouldPropagateException()
    {
        var userId = 999;

        _userServiceMock
            .Setup(x => x.GetUserByIdAsync(userId))
            .ThrowsAsync(new KeyNotFoundException($"User {userId} not found."));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.GetById(userId));

        Assert.Equal("User 999 not found.", exception.Message);

        _userServiceMock.Verify(
            x => x.GetUserByIdAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task Update_UserExists_ShouldReturnOk()
    {
        var userId = 1;

        var request = new UpdateUserRequest(
            "Jane Doe",
            "9123456789",
            true,
            "CUSTOMER");

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(userId, request))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(userId, request);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);

        _userServiceMock.Verify(
            x => x.UpdateUserAsync(userId, request),
            Times.Once);
    }

    [Fact]
    public async Task Update_UserDoesNotExist_ShouldPropagateException()
    {
        var userId = 999;

        var request = new UpdateUserRequest(
            "Jane Doe",
            "9123456789",
            true,
            "CUSTOMER");

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(userId, request))
            .ThrowsAsync(new KeyNotFoundException($"User {userId} not found."));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.Update(userId, request));

        Assert.Equal("User 999 not found.", exception.Message);

        _userServiceMock.Verify(
            x => x.UpdateUserAsync(userId, request),
            Times.Once);
    }

    [Fact]
    public async Task Delete_UserExists_ShouldReturnOk()
    {
        var userId = 1;

        _userServiceMock
            .Setup(x => x.DeleteUserAsync(userId))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(userId);

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(200, okResult.StatusCode);

        _userServiceMock.Verify(
            x => x.DeleteUserAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task Delete_UserDoesNotExist_ShouldPropagateException()
    {
        var userId = 999;

        _userServiceMock
            .Setup(x => x.DeleteUserAsync(userId))
            .ThrowsAsync(new KeyNotFoundException($"User {userId} not found."));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.Delete(userId));

        Assert.Equal("User 999 not found.", exception.Message);

        _userServiceMock.Verify(
            x => x.DeleteUserAsync(userId),
            Times.Once);
    }
}