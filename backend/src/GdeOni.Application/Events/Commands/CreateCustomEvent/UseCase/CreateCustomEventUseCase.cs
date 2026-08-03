using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Events.Commands.CreateCustomEvent.Model;
using GdeOni.Domain.Aggregates.Events;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Commands.CreateCustomEvent.UseCase;

/// <summary>Создаёт ручное событие текущего пользователя.</summary>
public sealed class CreateCustomEventUseCase(
    ICustomEventRepository repository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : ICreateCustomEventUseCase
{
    public async Task<Result<CreateCustomEventResponse, Error>> Execute(
        CreateCustomEventCommand command, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var eventResult = CustomEvent.Create(
            userIdResult.Value, command.Title, command.Date, command.LeadDays, now);
        if (eventResult.IsFailure)
            return eventResult.Error;

        await repository.Add(eventResult.Value, cancellationToken);
        await repository.Save(cancellationToken);

        return Result.Success<CreateCustomEventResponse, Error>(
            new CreateCustomEventResponse(eventResult.Value.Id));
    }
}
