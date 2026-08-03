using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Queries.GetRelativesSummary.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetRelativesSummary.UseCase;

public interface IGetRelativesSummaryUseCase
{
    Task<Result<RelativesSummaryResponse, Error>> Execute(CancellationToken cancellationToken);
}
