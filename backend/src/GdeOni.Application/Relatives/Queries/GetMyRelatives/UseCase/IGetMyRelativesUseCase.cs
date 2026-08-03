using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Queries.GetMyRelatives.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetMyRelatives.UseCase;

public interface IGetMyRelativesUseCase
{
    Task<Result<GetMyRelativesResponse, Error>> Execute(CancellationToken cancellationToken);
}
