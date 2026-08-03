using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.DeleteCustomEvent.UseCase;

/// <summary>Удаляет ручное событие (только своё).</summary>
public sealed class DeleteCustomEventUseCase(
    ICustomEventRepository repository,
    ICurrentUserService currentUserService)
    : IDeleteCustomEventUseCase
{
    public async Task<UnitResult<Error>> Execute(Guid id, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var customEvent = await repository.GetByIdForUser(
            id, userIdResult.Value, cancellationToken);
        if (customEvent is null)
            return Errors.Event.NotFound();

        repository.Delete(customEvent);
        await repository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
