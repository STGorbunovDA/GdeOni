using CSharpFunctionalExtensions;
using GdeOni.Application.Auth.Refresh.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Refresh.UseCase;

public interface IRefreshUseCase
{
    Task<Result<RefreshResponse, Error>> Execute(
        RefreshCommand command,
        CancellationToken cancellationToken);
}
