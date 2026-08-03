using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Commands.EditMessage.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.EditMessage.UseCase;

/// <summary>Правка своего последнего сообщения (пока собеседник не ответил).</summary>
public sealed class EditMessageUseCase(
    IRelativeConversationRepository conversationRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IEditMessageUseCase
{
    public async Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        EditMessageCommand command, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var me = userIdResult.Value;
        var conversation = await conversationRepository.GetByIdWithMessages(
            command.ConversationId, cancellationToken);
        if (conversation is null)
            return Errors.Relatives.ConversationNotFound();

        var editResult = conversation.EditMessage(
            command.MessageId, me, command.Text, timeProvider.GetUtcNow().UtcDateTime);
        if (editResult.IsFailure)
            return editResult.Error;

        await conversationRepository.Save(cancellationToken);

        var header = await conversationRepository.GetHeader(
            conversation.DeceasedId, conversation.OtherParticipant(me), cancellationToken);
        if (header is null)
            return Errors.Relatives.ConversationNotFound();

        return RelativeConversationMapper.ToDetail(conversation, header, me);
    }
}
