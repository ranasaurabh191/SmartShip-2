using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Persistence;
using SmartShip.Identity.Application.Interfaces.Repositories;
using SmartShip.Identity.Application.Services;
using SmartShip.Identity.Domain.Entities;
using SmartShip.Shared.Events;
using Xunit;

namespace SmartShip.Identity.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IPublishEndpoint> _publisherMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _publisherMock = new Mock<IPublishEndpoint>();

        _userService = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserExists_ShouldReturnUserDto()
    {
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            Role = "CUSTOMER",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        var result = await _userService.GetUserByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Phone, result.Phone);
        Assert.Equal(user.Role, result.Role);
        Assert.Equal(user.IsActive, result.IsActive);
        Assert.Equal(user.CreatedAt, result.CreatedAt);

        _userRepositoryMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.GetUserByIdAsync(999));

        Assert.Equal("User 999 not found.", exception.Message);

        _userRepositoryMock.Verify(
            x => x.GetByIdAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_UserExists_ShouldUpdateUserAndSaveChanges()
    {
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            Role = "CUSTOMER",
            IsActive = true
        };

        var request = new UpdateUserRequest(false);
            
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _userService.UpdateUserAsync(1, request);

        Assert.False(user.IsActive);

        _userRepositoryMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);

        _userRepositoryMock.Verify(
            x => x.Update(user),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_UserDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var request = new UpdateUserRequest(true);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.UpdateUserAsync(999, request));

        Assert.Equal("User 999 not found.", exception.Message);

        _userRepositoryMock.Verify(
            x => x.Update(It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_UserExists_ShouldDeleteSaveAndPublishEvent()
    {
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "9876543210",
            Role = "CUSTOMER",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _userService.DeleteUserAsync(1);

        _userRepositoryMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);

        _userRepositoryMock.Verify(
            x => x.Delete(user),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _publisherMock.Verify(
            x => x.Publish(
                It.Is<UserDeletedEvent>(e =>
                    e.UserId == 1 &&
                    e.Email == "john@example.com" &&
                    e.Role == "CUSTOMER"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_UserDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.DeleteUserAsync(999));

        Assert.Equal("User 999 not found.", exception.Message);

        _userRepositoryMock.Verify(
            x => x.Delete(It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _publisherMock.Verify(
            x => x.Publish(
                It.IsAny<UserDeletedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}