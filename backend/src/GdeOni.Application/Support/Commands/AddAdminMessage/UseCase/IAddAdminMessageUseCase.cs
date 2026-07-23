using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.AddAdminMessage.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.AddAdminMessage.UseCase;

public interface IAddAdminMessageUseCase
{
    Task<UnitResult<Error>> Execute(
        AddAdminMessageCommand command,
        CancellationToken cancellationToken);
}
