using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetById.UseCase;

public interface IGetUserByIdUseCase
{
    Task<Result<GetUserByIdResponse, Error>> Execute(
        GetUserByIdQuery query,
        CancellationToken cancellationToken);
}