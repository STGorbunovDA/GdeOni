using CSharpFunctionalExtensions;
using GdeOni.Application.Deceased.Create.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Deceased.Create.UseCase;

public interface ICreateDeceasedUseCase
{
    Task<Result<CreateDeceasedResponse, Error>> Execute(
        CreateDeceasedRequest request,
        CancellationToken cancellationToken);
}