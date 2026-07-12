using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Admin.Queries.GetAdminStats.Model;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Admin.Queries.GetAdminStats.UseCase;

/// <summary>
/// F38. Справка по системе для админа.
///
/// Роль проверяется и здесь, а не только атрибутом [Authorize(Roles=…)] на
/// контроллере: use case — это граница домена, и она не должна зависеть от
/// того, что кто-то не забыл повесить атрибут.
/// </summary>
public sealed class GetAdminStatsUseCase(
    IAdminStatsRepository statsRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetAdminStatsUseCase
{
    public Task<Result<AdminStatsResponse, Error>> Execute(
        GetAdminStatsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<AdminStatsResponse, Error>> Handle(
        GetAdminStatsQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        if (!currentUserService.IsAdmin())
            return Errors.User.UserForbidden();

        var stats = await statsRepository.GetStats(cancellationToken);
        return Result.Success<AdminStatsResponse, Error>(stats);
    }
}
