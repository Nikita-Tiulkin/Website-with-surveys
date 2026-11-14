
using Microsoft.EntityFrameworkCore;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;
using PollingSystem.Data.Enums;

namespace PollingSystem.Data.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User> AddAsync(
        CreateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        User user = new User
        {
            Login = dto.Login.Trim(),
            Email = dto.Email.Trim(),
            NormalizedEmail = dto.Email.Trim().ToUpperInvariant(),
            PasswordHash = dto.PasswordHash,
            Role = UserRole.User
        };

        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (email is null)
        {
            throw new ArgumentNullException(nameof(email));
        }

        string normalizedEmail = email.Trim().ToUpperInvariant();

        return context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        if (login is null)
        {
            throw new ArgumentNullException(nameof(login));
        }

        string normalizedLogin = login.Trim().ToUpperInvariant();

        return context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Login.ToUpper() == normalizedLogin,
                cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }
}
