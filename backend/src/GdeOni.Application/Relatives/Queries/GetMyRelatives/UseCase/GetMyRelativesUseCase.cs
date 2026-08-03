using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Queries.GetMyRelatives.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetMyRelatives.UseCase;

/// <summary>
/// Функция «Родственники». Отдаёт вживую список «родственников» текущего
/// пользователя (см. IRelativesRepository). Ночной джоб (Фаза 4) на список
/// не влияет — он только шлёт уведомления о новых.
/// </summary>
public sealed class GetMyRelativesUseCase(
    IRelativesRepository relativesRepository,
    ICurrentUserService currentUserService)
    : IGetMyRelativesUseCase
{
    public async Task<Result<GetMyRelativesResponse, Error>> Execute(
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var matches = await relativesRepository.GetRelativesForUser(
            userIdResult.Value, cancellationToken);

        var items = matches
            .Select(m => new MyRelativeItemResponse(
                m.DeceasedId,
                m.DeceasedFullName,
                m.BirthDate,
                m.DeathDate,
                m.RelativeUserId,
                m.RelativeUserName,
                m.RelationshipType.ToString()))
            .ToList();

        return Result.Success<GetMyRelativesResponse, Error>(
            new GetMyRelativesResponse(items));
    }
}
