using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PollingSystem.Api.Models;
using PollingSystem.Api.Services.Interfaces;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;
using PollingSystem.Data.Repositories;

namespace PollingSystem.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository users,
        IPasswordHasher<User> hasher,
        IConfiguration configuration)
    {
        _users = users;
        _hasher = hasher;
        _configuration = configuration;
    }

    public async Task<AuthOperationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        string email = request.Email.Trim();
        string login = request.Name.Trim();

        User? existingEmail = await _users.GetByEmailAsync(
            email, cancellationToken);

        if (existingEmail is not null)
        {
            return new AuthOperationResult
            {
                Error = "Пользователь с такой почтой уже существует.",
                IsConflict = true
            };
        }

        User? existingLogin = await _users.GetByLoginAsync(
            login, cancellationToken);

        if (existingLogin is not null)
        {
            return new AuthOperationResult
            {
                Error = "Пользователь с таким именем уже существует.",
                IsConflict = true
            };
        }

        User userForHash = new User
        {
            Login = login,
            Email = email
        };

        string passwordHash = _hasher.HashPassword(
            userForHash, request.Password);

        CreateUserDto createDto = new CreateUserDto
        {
            Login = login,
            Email = email,
            PasswordHash = passwordHash
        };

        User user = await _users.AddAsync(
            createDto, cancellationToken);

        return new AuthOperationResult
        {
            Response = CreateAuthResponse(user)
        };
    }

    public async Task<AuthOperationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        User? user = await _users.GetByEmailAsync(
            request.Email, cancellationToken);

        if (user is null)
        {
            return new AuthOperationResult
            {
                Error = "Неверная почта или пароль."
            };
        }

        PasswordVerificationResult result =
            _hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return new AuthOperationResult
            {
                Error = "Неверная почта или пароль."
            };
        }

        return new AuthOperationResult
        {
            Response = CreateAuthResponse(user)
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User? user = await _users.GetByIdAsync(
            userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Name = user.Login,
            Email = user.Email
        };
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
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Login),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.Role.ToString())
        };

        SymmetricSecurityKey key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        SigningCredentials credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}