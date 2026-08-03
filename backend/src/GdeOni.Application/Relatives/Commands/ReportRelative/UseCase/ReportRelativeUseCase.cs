using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Relatives.Commands.ReportRelative.Model;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.ReportRelative.UseCase;

/// <summary>
/// Жалоба на собеседника (Фаза 5). Жаловаться можно только на участника
/// своего диалога — нарушитель и карточка берутся из диалога, поэтому
/// пожаловаться на постороннего нельзя. Дедуп: если активная (неразобранная)
/// жалоба на этого человека в этом диалоге уже есть — повторную не создаём.
/// </summary>
public sealed class ReportRelativeUseCase(
    IRelativeConversationRepository conversationRepository,
    IRelativeReportRepository reportRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IReportRelativeUseCase
{
    public async Task<Result<ReportRelativeResponse, Error>> Execute(
        ReportRelativeCommand command, CancellationToken cancellationToken)
    {
        var userIdResult = currentUserService.GetCurrentUserId();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var me = userIdResult.Value;

        var conversation = await conversationRepository.GetByIdWithMessages(
            command.ConversationId, cancellationToken);
        if (conversation is null)
            return Errors.Relatives.ConversationNotFound();

        if (!conversation.IsParticipant(me))
            return Errors.Relatives.NotParticipant();

        var reportedUserId = conversation.OtherParticipant(me);

        // Дедуп спама: активная жалоба на этого человека в этом диалоге уже есть.
        if (await reportRepository.HasPendingReport(
                me, reportedUserId, conversation.Id, cancellationToken))
        {
            return Result.Success<ReportRelativeResponse, Error>(
                new ReportRelativeResponse(Created: false));
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reportResult = RelativeReport.Create(
            me, reportedUserId, conversation.DeceasedId, conversation.Id, command.Reason, now);
        if (reportResult.IsFailure)
            return reportResult.Error;

        await reportRepository.Add(reportResult.Value, cancellationToken);
        await reportRepository.Save(cancellationToken);

        return Result.Success<ReportRelativeResponse, Error>(
            new ReportRelativeResponse(Created: true));
    }
}
