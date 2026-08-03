using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Events.Commands.UpdateCustomEvent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.UpdateCustomEvent.UseCase;

/// <summary>Обновляет ручное событие (только своё).</summary>
public sealed class UpdateCustomEventUseCase(
    ICustomEventRepository repository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IUpdateCustomEventUseCase
{
    public async Task<UnitResult<Error>> Execute(
        UpdateCustomEventCommand command, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var customEvent = await repository.GetByIdForUser(
            command.Id, userIdResult.Value, cancellationToken);
        if (customEvent is null)
            return Errors.Event.NotFound();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var updateResult = customEvent.Update(
            command.Title, command.Date, command.LeadDays, now);
        if (updateResult.IsFailure)
            return updateResult.Error;

        await repository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
