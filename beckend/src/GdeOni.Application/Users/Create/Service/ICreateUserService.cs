using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Create.Model;

namespace GdeOni.Application.Users.Create.Service;

public interface ICreateUserService
{
    Task<Result<CreateUserResponse>> ExecuteAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);
}