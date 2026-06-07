using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Queries.GetAll.UseCase;

public interface IGetAllSupportTicketsUseCase
{
    Task<Result<GetAllSupportTicketsResponse, Error>> Execute(
        GetAllSupportTicketsQuery query,
        CancellationToken cancellationToken);
}
