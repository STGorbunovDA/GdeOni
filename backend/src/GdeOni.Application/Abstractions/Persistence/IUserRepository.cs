using GdeOni.Application.Users.GetAll.Model;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetById(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByIdWithTracking(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByEmail(string email, CancellationToken cancellationToken);
    Task<(List<User> Items, int TotalCount)> GetPaged(GetAllUsersQuery query, CancellationToken cancellationToken);
    Task<bool> ExistsById(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByUserName(string userName, CancellationToken cancellationToken);
    void Delete(User user);
    Task Add(User user, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}