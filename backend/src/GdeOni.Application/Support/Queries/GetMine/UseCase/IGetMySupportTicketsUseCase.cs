using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Queries.GetMine.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetMine.UseCase;

public interface IGetMySupportTicketsUseCase
{
    Task<Result<GetMySupportTicketsResponse, Error>> Execute(
        GetMySupportTicketsQuery query,
        CancellationToken cancellationToken);
}
