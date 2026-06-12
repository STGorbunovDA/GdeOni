using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Storage;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Support.Queries.GetAttachmentById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetAttachmentById.UseCase;

/// <summary>
/// D33. Возвращает presigned URL вложения для скачивания/просмотра.
/// Юзеру выдаётся только вложение его собственного тикета — иначе
/// AttachmentNotFound (а не Forbidden — не подсвечиваем существование
/// чужих файлов). Админу выдаётся любое вложение.
/// </summary>
public sealed class GetSupportAttachmentByIdUseCase(
    ISupportTicketRepository ticketRepository,
    IFileStorage fileStorage,
    ICurrentUserService currentUserService)
    : IGetSupportAttachmentByIdUseCase
{
    private static readonly TimeSpan PresignedTtl = TimeSpan.FromHours(1);

    public async Task<Result<GetSupportAttachmentByIdResponse, Error>> Execute(
        GetSupportAttachmentByIdQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var ticket = await ticketRepository.GetByIdWithAttachments(query.TicketId, cancellationToken);
        if (ticket is null)
            return Errors.Support.AttachmentNotFound();

        var isAdmin = currentUserService.IsAdmin();
        var isOwner = ticket.UserId is { } owner && owner == currentUserIdResult.Value;
        if (!isAdmin && !isOwner)
            return Errors.Support.AttachmentNotFound();

        var attachment = ticket.Attachments.FirstOrDefault(a => a.Id == query.AttachmentId);
        if (attachment is null)
            return Errors.Support.AttachmentNotFound();

        var url = await fileStorage.GetPresignedUrlAsync(
            attachment.Bucket,
            attachment.StorageKey,
            PresignedTtl,
            cancellationToken);

        return Result.Success<GetSupportAttachmentByIdResponse, Error>(
            new GetSupportAttachmentByIdResponse(
                attachment.Id,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.SizeBytes,
                url));
    }
}
