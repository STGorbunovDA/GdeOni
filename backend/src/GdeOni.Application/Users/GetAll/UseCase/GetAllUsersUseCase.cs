using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Users.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetAll.UseCase;

public sealed class GetAllUsersUseCase(
    IUserRepository userRepository,
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
        var (items, totalCount) = await userRepository.GetPaged(query, cancellationToken);

        var responseItems = items.Select(x => new GetAllUsersResponse
        {
            Id = x.Id,
            FullName = x.FullName,
            Email = x.Email,
            UserName = x.UserName,
            Role = x.Role.ToString(),
            RegisteredAtUtc = x.RegisteredAtUtc,
            LastLoginAtUtc = x.LastLoginAtUtc,
            TrackingCount = x.TrackedDeceasedItems.Count
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