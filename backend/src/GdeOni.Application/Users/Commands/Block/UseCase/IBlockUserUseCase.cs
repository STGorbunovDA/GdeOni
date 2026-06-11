using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.Block.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.Block.UseCase;

public interface IBlockUserUseCase
{
    Task<Result<BlockUserResponse, Error>> Execute(
        BlockUserCommand command,
        CancellationToken cancellationToken);
}
