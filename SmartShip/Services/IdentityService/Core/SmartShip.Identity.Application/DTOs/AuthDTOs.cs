
// Defines request and response DTOs used by the Identity module for authentication,
// user registration, profile management, and administrative user operations.
// API controls exactly what the client receives.

namespace SmartShip.Identity.Application.DTOs;

public record SignupRequest( string Name, string Email, string Phone, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Role, string Name, int UserId);
public record UserDto(int Id, string Name, string Email, string Phone, string Role, bool IsActive, DateTime CreatedAt);
public record UpdateUserRequest(bool IsActive);
public record UpdateMyProfileRequest(string Name, string Email, string Phone);