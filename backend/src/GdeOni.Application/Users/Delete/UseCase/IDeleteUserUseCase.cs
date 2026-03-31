using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Delete.UseCase;

public interface IDeleteUserUseCase
{
    Task<UnitResult<Error>> Execute(Guid id, CancellationToken cancellationToken);
}