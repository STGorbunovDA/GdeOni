using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.AddAdminMessage.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.AddAdminMessage.UseCase;

/// <summary>
/// D44. Админ отвечает в обращении, не меняя статус.
///
/// Раньше его сообщение можно было создать только побочным эффектом
/// резолюции или принудительного закрытия — то есть «спросить
/// уточнение» означало соврать статусом «Решено». Теперь ответ и
/// смена статуса — разные действия.
///
/// Роль проверяется и здесь, а не только атрибутом на контроллере —
/// defense in depth, как в остальных админских use case'ах.
/// </summary>
public sealed class AddAdminMessageUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IAddAdminMessageUseCase
{
    public Task<UnitResult<Error>> Execute(
        AddAdminMessageCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        AddAdminMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsSuperAdmin())
            return Errors.User.UserForbidden();

        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticket = await ticketRepository.GetById(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.General.NotFound("support_ticket", command.TicketId);

        var result = ticket.AddAdminMessage(
            currentUserIdResult.Value,
            command.Text,
            timeProvider.GetUtcNow().UtcDateTime);

        if (result.IsFailure)
            return result.Error;

        await ticketRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
