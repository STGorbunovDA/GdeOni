using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Queries.IsTrackedByMe.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.IsTrackedByMe.UseCase;

public interface IIsTrackedByMeUseCase
{
    Task<Result<IsTrackedByMeResponse, Error>> Execute(
        IsTrackedByMeQuery query,
        CancellationToken cancellationToken);
}
