using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShip.Identity.Application.DTOs;
using SmartShip.Identity.Application.Interfaces.Services;

namespace SmartShip.Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        var result = await _authService.SignupAsync(request);
        return Ok(result);
    }

    [HttpPost("debug-login")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugLogin([FromBody] LoginRequest request) =>
        Ok(await _authService.DebugLoginAsync(request));

    [HttpGet("fix-admin")]
    public async Task<IActionResult> FixAdmin() =>
        Ok(await _authService.FixAdminAsync());
}
