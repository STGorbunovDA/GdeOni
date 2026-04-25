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
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
            return Errors.General.Unauthorized();

        var currentUserId = currentUserService.UserId.Value;
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(),
            UserRole.Admin.ToString());
        
        var user = await userRepository.GetByIdWithTracking(query.UserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", query.UserId);
        
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
            TrackingCount = user.TrackedDeceasedItems.Count
        });
    }
}