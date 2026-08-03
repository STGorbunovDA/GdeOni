using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Events.Common;
using GdeOni.Application.Events.Queries.GetMyCustomEvents.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Queries.GetMyCustomEvents.UseCase;

/// <summary>Ручные события текущего пользователя (приватные).</summary>
public sealed class GetMyCustomEventsUseCase(
    ICustomEventRepository repository,
    ICurrentUserService currentUserService)
    : IGetMyCustomEventsUseCase
{
    public async Task<Result<GetMyCustomEventsResponse, Error>> Execute(
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var events = await repository.ListForUser(userIdResult.Value, cancellationToken);

        var items = events
            .OrderBy(e => e.EventDate.Month)
            .ThenBy(e => e.EventDate.Day)
            .Select(e => new CustomEventDto(e.Id, e.Title, e.EventDate, e.LeadDays))
            .ToList();

        return Result.Success<GetMyCustomEventsResponse, Error>(
            new GetMyCustomEventsResponse(items));
    }
}
