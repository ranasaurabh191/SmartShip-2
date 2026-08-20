using MassTransit;
using Microsoft.Extensions.Logging;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Persistence;
using SmartShip.Identity.Application.Interfaces.Repositories;
using SmartShip.Identity.Application.Interfaces.Services;
using SmartShip.Shared.Events;

namespace SmartShip.Identity.Application.Services;

/// Service providing administrative user lookup, state updates, account deletion, and event publishing.
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IPublishEndpoint _publisher;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IPublishEndpoint publisher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _publisher = publisher;
    }
    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        _logger.LogInformation("Fetching user with ID: {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            _logger.LogWarning("User not found with ID: {UserId}", id);
            throw new KeyNotFoundException($"User {id} not found.");
        }

        _logger.LogInformation("User found: {Email}", user.Email);

        return new UserDto(user.Id, user.Name, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt);
    }

    public async Task UpdateUserAsync(int id, UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            _logger.LogWarning("Update failed - user not found: {UserId}", id);
            throw new KeyNotFoundException($"User {id} not found.");
        }
        if (user.Role == "ADMIN")
        {
            _logger.LogWarning("Update failed, Tried to Update ADMIN - ADMIN cannot be Updated");
            throw new InvalidOperationException($"ADMIN cannot be Updated.");
        }

        user.IsActive = request.IsActive;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User updated successfully: {UserId}", id);
    }
    public async Task DeleteUserAsync(int userId)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Delete failed - user not found: {UserId}", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }
        if (user.Role == "ADMIN")
        {
            _logger.LogWarning("Delete failed, Tried to Delete ADMIN - ADMIN cannot be deleted");
            throw new InvalidOperationException($"ADMIN cannot be deleted.");
        }

        _userRepository.Delete(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully: {UserId}", userId);

        // Broadcast UserDeletedEvent across message bus to trigger cleanup in subscriber microservices
        await _publisher.Publish(new UserDeletedEvent
        {
            UserId = userId,
            Email = user.Email,
            Role = user.Role,
            DeletedAt = DateTime.Now,
        });

        _logger.LogInformation("Delete Event published successfully for User Id : {UserId}", userId);
    }
}