using CSharpFunctionalExtensions;
using GdeOni.Application.Admin.Queries.GetAdminStats.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Admin.Queries.GetAdminStats.UseCase;

public interface IGetAdminStatsUseCase
{
    Task<Result<AdminStatsResponse, Error>> Execute(
        GetAdminStatsQuery query,
        CancellationToken cancellationToken);
}
