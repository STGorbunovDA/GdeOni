using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Create.UseCase;

public interface ICreateDeceasedUseCase
{
    Task<Result<CreateDeceasedResponse, Error>> Execute(
        CreateDeceasedRequest request,
        CancellationToken cancellationToken);
}