using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetById.UseCase;

public interface IGetSupportTicketByIdUseCase
{
    Task<Result<GetSupportTicketByIdResponse, Error>> Execute(
        GetSupportTicketByIdQuery query,
        CancellationToken cancellationToken);
}
