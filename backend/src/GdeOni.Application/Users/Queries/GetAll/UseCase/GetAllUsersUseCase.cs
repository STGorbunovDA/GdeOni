using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetAll.UseCase;

public sealed class GetAllUsersUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetAllUsersUseCase
{
    public Task<Result<PagedResponse<GetAllUsersResponse>, Error>> Execute(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(
            query,
            Handle,
            cancellationToken);
    }

    private async Task<Result<PagedResponse<GetAllUsersResponse>, Error>> Handle(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.User.InsufficientPermissionsToViewAllUsers();

        // SuperAdmin'ы видны только другому SuperAdmin'у. Admin не должен
        // знать про SuperAdmin'ов через UI — иначе мог бы пытаться их
        // понизить/изменить (бэкэндом всё равно отрежется, но UX-проблема).
        var includeSuperAdmins = currentUserService.IsInRole(UserRole.SuperAdmin.ToString());

        var (items, totalCount) = await userRepository.GetPaged(query, includeSuperAdmins, cancellationToken);

        var responseItems = items.Select(row => new GetAllUsersResponse
        {
            Id = row.User.Id,
            FullName = row.User.FullName,
            Email = row.User.Email,
            UserName = row.User.UserName,
            Role = row.User.Role.ToString(),
            RegisteredAtUtc = row.User.RegisteredAtUtc,
            LastLoginAtUtc = row.User.LastLoginAtUtc,
            TrackingCount = row.TrackingCount
        }).ToList();

        var response = new PagedResponse<GetAllUsersResponse>
        {
            Items = responseItems,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Result.Success<PagedResponse<GetAllUsersResponse>, Error>(response);
    }
}