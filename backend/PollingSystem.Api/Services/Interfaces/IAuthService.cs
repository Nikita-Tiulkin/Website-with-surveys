using PollingSystem.Api.Models;
using PollingSystem.Data.DTOs;

namespace PollingSystem.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthOperationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}