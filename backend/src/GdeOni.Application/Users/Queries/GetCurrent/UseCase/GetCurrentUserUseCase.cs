using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.GetCurrent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetCurrent.UseCase;

public sealed class GetCurrentUserUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : IGetCurrentUserUseCase
{
    public async Task<Result<GetCurrentUserResponse, Error>> Execute(CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetById(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        return Result.Success<GetCurrentUserResponse, Error>(new GetCurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.Role.ToString()
        });
    }
}
