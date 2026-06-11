using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.UseCase;

public sealed class ClearBurialLocationUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IClearBurialLocationUseCase
{
    public Task<Result<ClearBurialLocationResponse, Error>> Execute(
        ClearBurialLocationCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<ClearBurialLocationResponse, Error>> Handle(
        ClearBurialLocationCommand command,
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
            return Errors.Deceased.ClearBurialLocationForbidden();

        if (deceased.BurialLocation is null)
            return Errors.Deceased.BurialLocationAlreadyNull();

        var result = deceased.ChangeBurialLocation(null);
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<ClearBurialLocationResponse, Error>(
            new ClearBurialLocationResponse(command.DeceasedId));
    }
}
