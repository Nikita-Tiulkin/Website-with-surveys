
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PollingSystem.Api.Models;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;
using PollingSystem.Data.Repositories;

namespace PollingSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IConfiguration _configuration;

    public AuthController(
        IUserRepository users,
        IPasswordHasher<User> hasher,
        IConfiguration configuration)
    {
        _users = users;
        _hasher = hasher;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        User? existingEmail = await _users.GetByEmailAsync(
            request.Email, cancellationToken);

        if (existingEmail is not null)
        {
            return Conflict(new
            {
                error = "Пользователь с такой почтой уже существует."
            });
        }

        User? existingLogin = await _users.GetByLoginAsync(
            request.Name, cancellationToken);

        if (existingLogin is not null)
        {
            return Conflict(new
            {
                error = "Пользователь с таким именем уже существует."
            });
        }

        User userForHash = new User
        {
            Login = request.Name.Trim(),
            Email = request.Email.Trim()
        };

        string passwordHash = _hasher.HashPassword(
            userForHash, request.Password);

        CreateUserDto createDto = new CreateUserDto
        {
            Login = request.Name.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = passwordHash
        };

        User user = await _users.AddAsync(createDto, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            CreateAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        User? user = await _users.GetByEmailAsync(
            request.Email, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new
            {
                error = "Неверная почта или пароль."
            });
        }

        PasswordVerificationResult result = _hasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                error = "Неверная почта или пароль."
            });
        }

        return Ok(CreateAuthResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out Guid id))
        {
            return Unauthorized(new
            {
                error = "Некорректный токен."
            });
        }

        User? user = await _users.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                error = "Пользователь не найден."
            });
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Name = user.Login,
            Email = user.Email
        });
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        return new AuthResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Login,
                Email = user.Email
            },
            Token = GenerateToken(user)
        };
    }

    private string GenerateToken(User user)
    {
        string jwtKey = _configuration["JWT_SECRET"]!;
        string issuer = _configuration["Jwt:Issuer"]!;
        string audience = _configuration["Jwt:Audience"]!;

        int expiresMinutes = _configuration.GetValue<int>(
            "Jwt:ExpiresMinutes");

        Claim[] claims =
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        SymmetricSecurityKey key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        SigningCredentials credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
