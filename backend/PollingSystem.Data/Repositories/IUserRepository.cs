
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Repositories;

public interface IUserRepository
{
    Task<User> AddAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
