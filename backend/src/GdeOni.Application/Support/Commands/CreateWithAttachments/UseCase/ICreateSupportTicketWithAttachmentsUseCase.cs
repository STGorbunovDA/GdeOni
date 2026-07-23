using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.CreateWithAttachments.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.CreateWithAttachments.UseCase;

public interface ICreateSupportTicketWithAttachmentsUseCase
{
    Task<Result<CreateSupportTicketWithAttachmentsResponse, Error>> Execute(
        CreateSupportTicketWithAttachmentsCommand command,
        CancellationToken cancellationToken);
}
