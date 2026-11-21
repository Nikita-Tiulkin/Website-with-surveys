namespace PollingSystem.Api.Models;

public class AuthOperationResult
{
    public AuthResponse? Response { get; init; }

    public string? Error { get; init; }

    public bool IsConflict { get; init; }

    public bool IsSuccess => Response is not null;
}