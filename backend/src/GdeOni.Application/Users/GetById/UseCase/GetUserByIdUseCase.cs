using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetById.UseCase;

public sealed class GetUserByIdUseCase(IUserRepository userRepository)
    : IGetUserByIdUseCase
{
    public async Task<Result<GetUserByIdResponse, Error>> Execute(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        return Result.Success<GetUserByIdResponse, Error>(new GetUserByIdResponse
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            RegisteredAtUtc = user.RegisteredAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            TrackingCount = user.TrackedDeceasedItems.Count
        });
    }
}