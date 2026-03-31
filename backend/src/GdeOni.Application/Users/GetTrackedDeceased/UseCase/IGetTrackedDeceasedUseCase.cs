using CSharpFunctionalExtensions;
using GdeOni.Application.Users.GetTrackedDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetTrackedDeceased.UseCase;

public interface IGetTrackedDeceasedUseCase
{
    Task<Result<GetTrackedDeceasedResponse, Error>> Execute(
        Guid userId,
        CancellationToken cancellationToken);
}