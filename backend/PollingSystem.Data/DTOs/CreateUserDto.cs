namespace PollingSystem.Data.DTOs;

public class CreateUserDto
{
    public string Login { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}
