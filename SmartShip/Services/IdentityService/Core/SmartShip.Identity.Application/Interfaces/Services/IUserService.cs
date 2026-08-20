

using SmartShip.Identity.Application.DTOs;

namespace SmartShip.Identity.Application.Interfaces.Services;

/// Service interface providing user management functions for administrative control and identity queries.
public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int id);
    Task UpdateUserAsync(int id, UpdateUserRequest request);
    Task DeleteUserAsync(int id);
}
