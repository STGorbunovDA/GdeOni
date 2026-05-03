using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetById.UseCase;

public sealed class GetUserByIdUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetUserByIdUseCase
{
    public Task<Result<GetUserByIdResponse, Error>> Execute(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetUserByIdResponse, Error>> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        // D7.37: вместо Include(TrackedDeceasedItems) — узкая проекция
        // (User, COUNT subquery). Раньше тянули всю коллекцию подписок
        // ради одного .Count в response.
        var row = await userRepository.GetByIdWithTrackingCount(query.UserId, cancellationToken);
        if (row is null)
            return Errors.General.NotFound("user", query.UserId);

        var (user, trackingCount) = row.Value;

        if (!isAdmin && user.Id != currentUserId)
            return Errors.User.UserForbidden();

        return Result.Success<GetUserByIdResponse, Error>(new GetUserByIdResponse
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            RegisteredAtUtc = user.RegisteredAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            TrackingCount = trackingCount
        });
    }
}