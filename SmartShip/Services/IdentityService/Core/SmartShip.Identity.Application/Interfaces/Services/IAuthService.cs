
using SmartShip.Identity.Application.DTOs;

namespace SmartShip.Identity.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<object> DebugLoginAsync(LoginRequest request);  
    Task<object> FixAdminAsync();
    Task<object> RequestSignupOtpAsync(SignupOtpRequest request);  
    Task<OtpResponse> VerifySignupOtpAsync(VerifyOtpRequest request);
    Task<object> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<object> ResetPasswordAsync(ResetPasswordRequest request);
}
