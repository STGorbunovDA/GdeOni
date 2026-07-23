using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.AddUserMessage.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.AddUserMessage.UseCase;

/// <summary>
/// D44. Пользователь пишет сообщение в переписку своего обращения.
///
/// До D44 ответить можно было только переоткрыв тикет (Reopen), а он
/// требует статус Resolved — то есть пока обращение в работе, юзер был
/// нем. Теперь диалог идёт нормально; принадлежность тикета и
/// допустимость статуса проверяет домен.
/// </summary>
public sealed class AddUserMessageUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    TimeProvider timeProvider)
    : IAddUserMessageUseCase
{
    public Task<UnitResult<Error>> Execute(
        AddUserMessageCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<UnitResult<Error>> Handle(
        AddUserMessageCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticket = await ticketRepository.GetById(command.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.General.NotFound("support_ticket", command.TicketId);

        var result = ticket.AddUserMessage(
            currentUserIdResult.Value,
            command.Text,
            timeProvider.GetUtcNow().UtcDateTime);

        if (result.IsFailure)
            return result.Error;

        await ticketRepository.Save(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
