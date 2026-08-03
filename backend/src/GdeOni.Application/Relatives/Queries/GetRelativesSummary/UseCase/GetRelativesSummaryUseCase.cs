using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Queries.GetRelativesSummary.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetRelativesSummary.UseCase;

/// <summary>
/// Фаза 4. Сводка «Родственников» для попапа «События» и бейджа:
///  - новые родственники — из лога обнаружений (ночной джоб), ещё не
///    просмотренные и валидные по текущему состоянию;
///  - непрочитанные диалоги — считаются вживую из переписки.
/// </summary>
public sealed class GetRelativesSummaryUseCase(
    IRelativesRepository relativesRepository,
    IRelativeConversationRepository conversationRepository,
    ICurrentUserService currentUserService)
    : IGetRelativesSummaryUseCase
{
    public async Task<Result<RelativesSummaryResponse, Error>> Execute(
        CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var userId = userIdResult.Value;

        var newRelatives = await relativesRepository.GetNewRelatives(userId, cancellationToken);
        var conversations = await conversationRepository.GetConversationsForUser(
            userId, cancellationToken);

        var newItems = newRelatives
            .Select(r => new NewRelativeSummaryItem(
                r.DeceasedId,
                r.DeceasedFullName,
                r.RelativeUserId,
                r.RelativeUserName,
                r.RelationshipType.ToString()))
            .ToList();

        var unread = conversations
            .Where(c => c.UnreadCount > 0)
            .Select(c => new UnreadConversationItem(
                c.ConversationId,
                c.DeceasedId,
                c.DeceasedFullName,
                c.OtherUserId,
                c.OtherUserName,
                c.UnreadCount))
            .ToList();

        var response = new RelativesSummaryResponse(
            newItems,
            unread,
            unread.Sum(c => c.UnreadCount));

        return Result.Success<RelativesSummaryResponse, Error>(response);
    }
}
