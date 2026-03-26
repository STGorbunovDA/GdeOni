using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Update.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Update.UseCase;

public interface IUpdateDeceasedUseCase
{
    Task<Result<UpdateDeceasedResponse, Error>> Execute(
        UpdateDeceasedRequest request,
        CancellationToken cancellationToken);
}