
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PollingSystem.Api.Models;
using PollingSystem.Api.Services.Interfaces;
using PollingSystem.Data.DTOs;

namespace PollingSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        AuthOperationResult result =
            await _authService.RegisterAsync(
                request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.IsConflict)
            {
                return Conflict(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthOperationResult result =
            await _authService.LoginAsync(
                request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out Guid id))
        {
            return Unauthorized(new
            {
                error = "Некорректный токен."
            });
        }

        UserDto? user = await _authService.GetCurrentUserAsync(
            id, cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                error = "Пользователь не найден."
            });
        }

        return Ok(user);
    }
}
