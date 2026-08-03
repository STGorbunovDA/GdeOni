using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Common;
using GdeOni.Application.Relatives.Queries.GetConversation.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetConversation.UseCase;

/// <summary>
/// Открывает диалог: проверяет участника, помечает прочитанными сообщения
/// собеседника (это и есть «посмотрел»), отдаёт детали + чей ход.
/// </summary>
public sealed class GetConversationUseCase(
    IRelativeConversationRepository conversationRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IGetConversationUseCase
{
    public async Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        GetConversationQuery query, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var me = userIdResult.Value;
        var conversation = await conversationRepository.GetByIdWithMessages(
            query.ConversationId, cancellationToken);
        if (conversation is null)
            return Errors.Relatives.ConversationNotFound();

        if (!conversation.IsParticipant(me))
            return Errors.Relatives.NotParticipant();

        conversation.MarkReadBy(me, timeProvider.GetUtcNow().UtcDateTime);
        await conversationRepository.Save(cancellationToken);

        var header = await conversationRepository.GetHeader(
            conversation.DeceasedId, conversation.OtherParticipant(me), cancellationToken);
        if (header is null)
            return Errors.Relatives.ConversationNotFound();

        return RelativeConversationMapper.ToDetail(conversation, header, me);
    }
}
