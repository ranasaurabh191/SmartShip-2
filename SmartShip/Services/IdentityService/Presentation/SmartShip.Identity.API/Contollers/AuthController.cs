using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Services;
using System.Security.Claims;

namespace SmartShip.Identity.API.Controllers;

/// Web API controller exposing authentication, registration, user profile management, and internal identity validation endpoints.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _authService.SignupAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized(new { message = "Invalid user identity." });
        await _authService.UpdateMyProfileAsync(userId, request, cancellationToken);

        return Ok(new { message = "Profile updated successfully." });
    }

    [HttpPost("debug-login")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugLogin([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await _authService.DebugLoginAsync(request, cancellationToken));
    }

    [HttpGet("internal/users/{id}/exists")]
    public async Task<IActionResult> Exists(int id)
    {
        var exists = await _authService.ExistsActiveUserAsync(id);
        if (!exists) return NotFound();

        return Ok(new
        {
            exists = true
        });
    }
}
