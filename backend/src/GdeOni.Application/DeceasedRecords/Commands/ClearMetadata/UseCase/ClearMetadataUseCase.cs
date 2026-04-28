using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.UseCase;

public sealed class ClearMetadataUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IClearMetadataUseCase
{
    public Task<Result<ClearMetadataResponse, Error>> Execute(
        ClearMetadataCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ClearMetadataResponse, Error>> Handle(
        ClearMetadataCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();
        
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);
        
        if (!isAdmin && deceased.CreatedByUserId != currentUserId)
            return Errors.DeceasedMetadata.DeleteDeceasedMetadataForbidden();

        var result = deceased.ClearMetadata();
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<ClearMetadataResponse, Error>(
            new ClearMetadataResponse(command.DeceasedId));
    }
}