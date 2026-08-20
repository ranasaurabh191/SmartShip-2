using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Persistence;
using SmartShip.Identity.Application.Interfaces.Repositories;
using SmartShip.Identity.Application.Interfaces.Services;
using SmartShip.Identity.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartShip.Identity.Application.Services;

/// Service implementing authentication, registration, JWT token management, and profile updates for IdentityService.
/// Encapsulates credential hashing with BCrypt and JWT issuance logic.
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">Repository interface for accessing user data.</param>
    /// <param name="unitOfWork">Unit of work for executing atomic database operations.</param>
    /// <param name="config">Configuration settings containing JWT key, issuer, and audience.</param>
    /// <param name="logger">Logger for recording operational events.</param>
    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Generates a signed JSON Web Token (JWT) containing user ID, email, name, and role claims.
    /// </summary>
    /// <param name="user">The user entity for whom to generate the token.</param>
    /// <returns>A signed JWT token string.</returns>
    private string GenerateToken(User user)
    {
        _logger.LogInformation("Generating JWT token for user: {Email}, Role: {Role}", user.Email, user.Role);

        var jwt = _config.GetSection("JwtSettings");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };
         
        var expiryMinutes = double.Parse(jwt["ExpiryMinutes"]!);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        _logger.LogInformation("Token generated successfully for user: {Email}, expires in {Minutes} minutes", user.Email, expiryMinutes);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponse> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Signup attempt for email: {Email}", request.Email);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Signup failed - email already exists: {Email}", request.Email);
            throw new InvalidOperationException("User with this email already exists.");
        }

        // Hash plaintext password using BCrypt algorithm
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = passwordHash,
            Role = "CUSTOMER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User created successfully: {Email} (ID: {UserId})", user.Email, user.Id);

        return new AuthResponse(GenerateToken(user), user.Role, user.Name, user.Id);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            throw new KeyNotFoundException("User not found with this email. Please signup.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed - inactive user: {Email}", request.Email);
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        // Verify provided password against stored BCrypt hash
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - wrong password: {Email}", request.Email);
            throw new UnauthorizedAccessException("Incorrect password.");
        }

        _logger.LogInformation("Login successful: {Email} | Role: {Role}", user.Email, user.Role);
        return new AuthResponse(GenerateToken(user), user.Role, user.Name, user.Id);
    }

 
    public async Task<UserDto> UpdateMyProfileAsync(int userId, UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Updating profile for user ID: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Profile update failed - user not found: {UserId}", userId);

            throw new KeyNotFoundException($"User {userId} not found.");
        }

        var existingEmailUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existingEmailUser != null && existingEmailUser.Id != userId)
        {
            _logger.LogWarning("Profile update failed - email already exists: {Email}", request.Email);

            throw new InvalidOperationException("User with this email already exists.");
        }

        user.Name = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;

        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Profile updated successfully for user ID: {UserId}", userId);

        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Role,
            user.IsActive,
            user.CreatedAt);
    }

    public async Task<object> DebugLoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Debug login attempt for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Debug login failed - user not found: {Email}", request.Email);
            return new { step = "FAILED", reason = "User not found", email = request.Email };
        }

        var hashMatch = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (hashMatch)
            _logger.LogInformation("Debug login SUCCESS for: {Email}", user.Email);
        else
            _logger.LogWarning("Debug login FAILED - wrong password: {Email}", user.Email);

        return new
        {
            step = hashMatch ? "SUCCESS" : "FAILED",
            reason = hashMatch ? "Password matches" : "Wrong password",
            email = user.Email,
            role = user.Role,
            isActive = user.IsActive
        };
    }

    public async Task<bool> ExistsActiveUserAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.ExistsActiveByIdAsync(id, cancellationToken);
    }
}