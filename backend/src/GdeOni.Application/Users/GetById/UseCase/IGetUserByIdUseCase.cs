using CSharpFunctionalExtensions;
using GdeOni.Application.Users.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetById.UseCase;

public interface IGetUserByIdUseCase
{
    Task<Result<GetUserByIdResponse, Error>> Execute(
        Guid userId,
        CancellationToken cancellationToken);
}