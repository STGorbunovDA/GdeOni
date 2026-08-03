using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Queries.GetRelativeReports.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetRelativeReports.UseCase;

public interface IGetRelativeReportsUseCase
{
    Task<Result<GetRelativeReportsResponse, Error>> Execute(
        GetRelativeReportsQuery query, CancellationToken cancellationToken);
}
