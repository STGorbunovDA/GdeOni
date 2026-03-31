using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Delete.UseCase;

public sealed class DeleteUserUseCase(IUserRepository userRepository)
    : IDeleteUserUseCase
{
    public async Task<UnitResult<Error>> Execute(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(id, cancellationToken);

        if (user is null)
            return Errors.General.NotFound("user", id);

        userRepository.Delete(user);
        await userRepository.Save(cancellationToken);

        return UnitResult.Success<Error>();
    }
}