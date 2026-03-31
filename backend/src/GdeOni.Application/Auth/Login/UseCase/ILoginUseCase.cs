using CSharpFunctionalExtensions;
using GdeOni.Application.Auth.Login.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.Login.UseCase;

public interface ILoginUseCase
{
    Task<Result<LoginResponse, Error>> Execute(
        LoginRequest request,
        CancellationToken cancellationToken);
}