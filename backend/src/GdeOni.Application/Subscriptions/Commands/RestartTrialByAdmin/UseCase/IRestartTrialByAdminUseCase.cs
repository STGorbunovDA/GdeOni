using CSharpFunctionalExtensions;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.UseCase;

public interface IRestartTrialByAdminUseCase
{
    Task<UnitResult<Error>> Execute(
        RestartTrialByAdminCommand command,
        CancellationToken cancellationToken);
}
