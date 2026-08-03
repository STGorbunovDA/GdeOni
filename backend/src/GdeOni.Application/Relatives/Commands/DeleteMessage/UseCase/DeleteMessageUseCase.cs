using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Commands.DeleteMessage.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.DeleteMessage.UseCase;

/// <summary>Удаление своего последнего сообщения (пока собеседник не ответил).</summary>
public sealed class DeleteMessageUseCase(
    IRelativeConversationRepository conversationRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IDeleteMessageUseCase
{
    public async Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        DeleteMessageCommand command, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var me = userIdResult.Value;
        var conversation = await conversationRepository.GetByIdWithMessages(
            command.ConversationId, cancellationToken);
        if (conversation is null)
            return Errors.Relatives.ConversationNotFound();

        var deleteResult = conversation.DeleteMessage(
            command.MessageId, me, timeProvider.GetUtcNow().UtcDateTime);
        if (deleteResult.IsFailure)
            return deleteResult.Error;

        await conversationRepository.Save(cancellationToken);

        var header = await conversationRepository.GetHeader(
            conversation.DeceasedId, conversation.OtherParticipant(me), cancellationToken);
        if (header is null)
            return Errors.Relatives.ConversationNotFound();

        return RelativeConversationMapper.ToDetail(conversation, header, me);
    }
}
