using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}