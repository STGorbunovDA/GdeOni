using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.SetRelativeConnectionsConsent.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.SetRelativeConnectionsConsent.UseCase;

public interface ISetRelativeConnectionsConsentUseCase
{
    Task<UnitResult<Error>> Execute(
        SetRelativeConnectionsConsentCommand command,
        CancellationToken cancellationToken);
}
