using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.UseCase;

public interface ISetBurialLocationFromGpsUseCase
{
    Task<Result<SetBurialLocationFromGpsResponse, Error>> Execute(
        SetBurialLocationFromGpsCommand command,
        CancellationToken cancellationToken);
}
