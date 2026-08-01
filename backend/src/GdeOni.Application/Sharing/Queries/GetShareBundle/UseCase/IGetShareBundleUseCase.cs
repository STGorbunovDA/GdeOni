using CSharpFunctionalExtensions;
using GdeOni.Application.Sharing.Queries.GetShareBundle.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Queries.GetShareBundle.UseCase;

public interface IGetShareBundleUseCase
{
    Task<Result<GetShareBundleResponse, Error>> Execute(
        GetShareBundleQuery query,
        CancellationToken cancellationToken);
}
