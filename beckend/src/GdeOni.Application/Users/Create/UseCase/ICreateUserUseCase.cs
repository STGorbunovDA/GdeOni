using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Create.UseCase;

public interface ICreateUserUseCase
{
    Task<Result<CreateUserResponse, Error>> Execute(
        CreateUserRequest request,
        CancellationToken cancellationToken);
}