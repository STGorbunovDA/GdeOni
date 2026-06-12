using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Queries.GetAttachmentById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetAttachmentById.UseCase;

public interface IGetSupportAttachmentByIdUseCase
{
    Task<Result<GetSupportAttachmentByIdResponse, Error>> Execute(
        GetSupportAttachmentByIdQuery query,
        CancellationToken cancellationToken);
}
