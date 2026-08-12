
using SmartShip.Identity.Application.DTOs;

namespace SmartShip.Identity.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> SignupAsync(SignupRequest request);
    Task<object> DebugLoginAsync(LoginRequest request);  
    Task<object> FixAdminAsync();
    
  
}
