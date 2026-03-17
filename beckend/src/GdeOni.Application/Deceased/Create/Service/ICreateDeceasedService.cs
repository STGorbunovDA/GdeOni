using CSharpFunctionalExtensions;
using GdeOni.Application.Deceased.Create.Model;

namespace GdeOni.Application.Deceased.Create.Service;

public interface ICreateDeceasedService
{
    Task<Result<CreateDeceasedResponse>> ExecuteAsync(
        CreateDeceasedRequest request,
        CancellationToken cancellationToken);
}