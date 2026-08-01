using CSharpFunctionalExtensions;
using GdeOni.Application.Sharing.Commands.ImportShareBundle.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Sharing.Commands.ImportShareBundle.UseCase;

public interface IImportShareBundleUseCase
{
    Task<Result<ImportShareBundleResponse, Error>> Execute(
        ImportShareBundleCommand command,
        CancellationToken cancellationToken);
}
