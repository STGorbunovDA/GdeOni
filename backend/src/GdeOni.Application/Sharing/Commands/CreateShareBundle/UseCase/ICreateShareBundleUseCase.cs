using CSharpFunctionalExtensions;
using GdeOni.Application.Sharing.Commands.CreateShareBundle.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Commands.CreateShareBundle.UseCase;

public interface ICreateShareBundleUseCase
{
    Task<Result<CreateShareBundleResponse, Error>> Execute(
        CreateShareBundleCommand command,
        CancellationToken cancellationToken);
}
