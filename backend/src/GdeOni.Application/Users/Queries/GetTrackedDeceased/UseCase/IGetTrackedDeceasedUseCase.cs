using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.UseCase;

public interface IGetTrackedDeceasedUseCase
{
    Task<Result<GetTrackedDeceasedResponse, Error>> Execute(
        CancellationToken cancellationToken);
}