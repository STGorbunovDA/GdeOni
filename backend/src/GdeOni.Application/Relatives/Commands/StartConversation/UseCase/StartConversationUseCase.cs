using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Commands.StartConversation.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.StartConversation.UseCase;

/// <summary>
/// Открывает диалог с родственником: если уже есть — возвращает его; иначе
/// проверяет право (IsRelative) и создаёт. Гонку параллельного создания
/// (уникальный индекс пары) ловим и перечитываем существующий.
/// </summary>
public sealed class StartConversationUseCase(
    IRelativeConversationRepository conversationRepository,
    IRelativesRepository relativesRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IStartConversationUseCase
{
    public async Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        StartConversationCommand command, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var me = userIdResult.Value;
        if (command.OtherUserId == me)
            return Errors.Relatives.CannotMessageSelf();

        var conversation = await conversationRepository.GetByParticipants(
            command.DeceasedId, me, command.OtherUserId, cancellationToken);

        if (conversation is null)
        {
            var allowed = await relativesRepository.IsRelative(
                me, command.OtherUserId, command.DeceasedId, cancellationToken);
            if (!allowed)
                return Errors.Relatives.CannotStartConversation();

            var createResult = RelativeConversation.Create(
                command.DeceasedId, me, command.OtherUserId,
                timeProvider.GetUtcNow().UtcDateTime);
            if (createResult.IsFailure)
                return createResult.Error;

            await conversationRepository.Add(createResult.Value, cancellationToken);
            try
            {
                await conversationRepository.Save(cancellationToken);
                conversation = createResult.Value;
            }
            catch (UniqueConstraintException)
            {
                // Параллельно уже создали — берём существующий.
                conversation = await conversationRepository.GetByParticipants(
                    command.DeceasedId, me, command.OtherUserId, cancellationToken);
                if (conversation is null)
                    return Errors.Relatives.ConversationNotFound();
            }
        }

        var header = await conversationRepository.GetHeader(
            conversation.DeceasedId, conversation.OtherParticipant(me), cancellationToken);
        if (header is null)
            return Errors.Relatives.ConversationNotFound();

        return RelativeConversationMapper.ToDetail(conversation, header, me);
    }
}
