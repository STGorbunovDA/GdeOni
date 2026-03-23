using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsById(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken);
    Task Add(User user, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}