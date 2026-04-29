using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Queries.GetCurrent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetCurrent.UseCase;

public interface IGetCurrentUserUseCase
{
    Task<Result<GetCurrentUserResponse, Error>> Execute(CancellationToken cancellationToken);
}
