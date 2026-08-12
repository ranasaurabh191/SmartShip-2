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

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly IPublishEndpoint _publisher;
    private readonly IConfiguration _configuration;


    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        ILogger<AuthService> logger,
        IPublishEndpoint publisher,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _config = config;
        _configuration = configuration;
        _logger = logger;
        _publisher = publisher;
    }


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
    public async Task<AuthResponse> SignupAsync(SignupRequest request)
    {
        _logger.LogInformation("Signup attempt for email: {Email}", request.Email);

        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Signup failed - email already exists: {Email}", request.Email);
            throw new InvalidOperationException("User with this email already exists.");
        }

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
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email);

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

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - wrong password: {Email}", request.Email);
            throw new UnauthorizedAccessException("Incorrect password.");
        }

        _logger.LogInformation("Login successful: {Email} | Role: {Role}", user.Email, user.Role);
        return new AuthResponse(GenerateToken(user), user.Role, user.Name, user.Id);
    }

    


    public async Task<object> DebugLoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Debug login attempt for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email);

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

    public async Task<object> FixAdminAsync()
    {

        _logger.LogInformation("FixAdmin operation started");

        var admin = await _userRepository.GetByEmailForAdminAsync("admin@smartship.com");
        var defaultPassword = _configuration["AdminSettings:DefaultPassword"];

        if (admin == null)
        {
            _logger.LogWarning("Admin not found. Creating new admin user.");
            admin = new User
            {
                Name = "Super Admin",
                Email = "admin@smartship.com",
                Phone = "9999999999",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                Role = "ADMIN",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _userRepository.AddAsync(admin);
        }
        else
        {
            _logger.LogWarning("Admin already exists. Resetting password.");

            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
            _userRepository.Update(admin);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("FixAdmin operation completed successfully.");

        return new { message = "Admin fixed successfully." };

    }

}