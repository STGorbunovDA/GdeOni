using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Notifications;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Commands.Create.Model;
using GdeOni.Domain.Aggregates.Notifications;
using GdeOni.Domain.Aggregates.Support;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.Create.UseCase;

/// <summary>
/// D25. Создание Manual-тикета юзером через форму "Обращение в
/// поддержку". Доступно любому authenticated юзеру, включая админов
/// (админ может тоже описать проблему). Severity всегда Normal —
/// апгрейдить может только админ через UpdateSeverity.
///
/// После сохранения уведомляем SuperAdmin'ов о новом обращении (F40 —
/// best-effort, сбой уведомления не влияет на создание тикета).
/// </summary>
public sealed class CreateSupportTicketUseCase(
    ISupportTicketRepository ticketRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor,
    INotificationService notificationService,
    TimeProvider timeProvider)
    : ICreateSupportTicketUseCase
{
    public Task<Result<CreateSupportTicketResponse, Error>> Execute(
        CreateSupportTicketCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<CreateSupportTicketResponse, Error>> Handle(
        CreateSupportTicketCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticketResult = SupportTicket.CreateManual(
            currentUserIdResult.Value,
            command.Kind,
            command.Title,
            command.Description,
            timeProvider.GetUtcNow().UtcDateTime);

        if (ticketResult.IsFailure)
            return ticketResult.Error;

        await ticketRepository.Add(ticketResult.Value, cancellationToken);
        await ticketRepository.Save(cancellationToken);

        await notificationService.NotifyRolesAsync(
            new[] { UserRole.SuperAdmin },
            NotificationKind.SupportTicketCreated,
            "Новое обращение",
            ticketResult.Value.Title,
            $"/admin/support-tickets/{ticketResult.Value.Id}",
            cancellationToken);

        return Result.Success<CreateSupportTicketResponse, Error>(
            new CreateSupportTicketResponse(ticketResult.Value.Id));
    }
}
