using PollingSystem.Data.Enums;
using PollingSystem.Data.DTOs;
using Microsoft.EntityFrameworkCore;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User> AddAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var user = new User
        {
            Login = dto.Login,
            Email = dto.Email,
            NormalizedEmail = dto.Email.Trim().ToUpperInvariant(),
            PasswordHash = dto.PasswordHash,
            Role = UserRole.User
        };

        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (email is null)
        {
            throw new ArgumentNullException(nameof(email));
        }
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return context.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }
}
