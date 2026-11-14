
namespace PollingSystem.Api.Models;

public class AuthResponse
{
    public UserDto User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
}
