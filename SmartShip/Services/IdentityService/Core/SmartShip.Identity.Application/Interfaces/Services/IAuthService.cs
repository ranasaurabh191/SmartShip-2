using SmartShip.Identity.Application.DTOs;

namespace SmartShip.Identity.Application.Interfaces.Services;

/// Service interface governing authentication workflows, user onboarding, JWT generation, and profile self-service operations.
public interface IAuthService
{
    Task<AuthResponse> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateMyProfileAsync(int userId, UpdateMyProfileRequest request, CancellationToken cancellationToken = default);
    Task<object> DebugLoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveUserAsync(int id, CancellationToken cancellationToken = default);
}
