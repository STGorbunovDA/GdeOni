using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.UseCase;

/// <summary>
/// Админ-листинг отслеживаемых юзера. Авторизация — Roles=SuperAdmin/Admin
/// на контроллере. SuperAdmin'ов админ не открывает — отрезается на уровне
/// GetUserById, но здесь для надёжности тоже отрезаем через guard в HTTP
/// слое (просто 404/403 не отдадим — Repository.GetMyTrackedDeceasedPaged
/// сама проверки роли не делает).
/// </summary>
public sealed class GetUserTrackedDeceasedForAdminUseCase(
    IUserRepository userRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetUserTrackedDeceasedForAdminUseCase
{
    public Task<Result<GetUserTrackedDeceasedForAdminResponse, Error>> Execute(
        GetUserTrackedDeceasedForAdminQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetUserTrackedDeceasedForAdminResponse, Error>> Handle(
        GetUserTrackedDeceasedForAdminQuery query,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await userRepository.GetMyTrackedDeceasedPaged(
            query.UserId, query.Page, query.PageSize, cancellationToken);

        var responseItems = items.Select(p => new UserTrackedDeceasedItem(
            p.Deceased.Id,
            p.Deceased.Name.FullName,
            p.Deceased.LifePeriod.BirthDate,
            p.Deceased.LifePeriod.DeathDate,
            p.Tracking.RelationshipType.ToString(),
            p.Tracking.Status.ToString(),
            p.Tracking.TrackedAtUtc)).ToList();

        return Result.Success<GetUserTrackedDeceasedForAdminResponse, Error>(
            new GetUserTrackedDeceasedForAdminResponse(
                responseItems, totalCount, query.Page, query.PageSize));
    }
}
