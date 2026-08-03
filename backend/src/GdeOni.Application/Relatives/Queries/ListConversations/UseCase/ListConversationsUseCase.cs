using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Queries.ListConversations.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.ListConversations.UseCase;

/// <summary>Список диалогов текущего пользователя (инбокс + непрочитанные).</summary>
public sealed class ListConversationsUseCase(
    IRelativeConversationRepository conversationRepository,
    ICurrentUserService currentUserService)
    : IListConversationsUseCase
{
    public async Task<Result<ListRelativeConversationsResponse, Error>> Execute(
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var summaries = await conversationRepository.GetConversationsForUser(
            userIdResult.Value, cancellationToken);

        var items = summaries
            .Select(s => new RelativeConversationSummaryResponse(
                s.ConversationId,
                s.DeceasedId,
                s.DeceasedFullName,
                s.OtherUserId,
                s.OtherUserName,
                s.OtherRelationship?.ToString(),
                s.LastMessageAtUtc,
                s.LastMessagePreview,
                s.LastMessageIsMine,
                s.UnreadCount,
                s.CanSend))
            .ToList();

        return Result.Success<ListRelativeConversationsResponse, Error>(
            new ListRelativeConversationsResponse(items));
    }
}
