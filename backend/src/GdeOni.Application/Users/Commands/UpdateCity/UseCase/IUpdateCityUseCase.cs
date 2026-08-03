using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.UpdateCity.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateCity.UseCase;

public interface IUpdateCityUseCase
{
    Task<UnitResult<Error>> Execute(UpdateCityCommand command, CancellationToken cancellationToken);
}
