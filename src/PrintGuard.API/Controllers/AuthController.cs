using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrintGuard.Application.DTOs;
using PrintGuard.Application.Interfaces;

namespace PrintGuard.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromHeader(Name = "X-Session-Token")] string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return Ok(ApiResponse<bool>.Ok(true, "Logged out"));

        var result = await _authService.LogoutAsync(sessionToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
            return Unauthorized(ApiResponse<UserDto>.Fail("Unauthorized"));

        var result = await _authService.GetCurrentUserAsync(userId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
