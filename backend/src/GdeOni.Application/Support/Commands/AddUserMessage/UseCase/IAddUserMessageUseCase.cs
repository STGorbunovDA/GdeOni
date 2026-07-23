using CSharpFunctionalExtensions;
using GdeOni.Application.Support.Commands.AddUserMessage.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Support.Commands.AddUserMessage.UseCase;

public interface IAddUserMessageUseCase
{
    Task<UnitResult<Error>> Execute(
        AddUserMessageCommand command,
        CancellationToken cancellationToken);
}
