using GdeOni.Domain.Aggregates.Auth;

namespace GdeOni.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHash(string tokenHash, CancellationToken cancellationToken);
    Task Add(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task Save(CancellationToken cancellationToken);
}
