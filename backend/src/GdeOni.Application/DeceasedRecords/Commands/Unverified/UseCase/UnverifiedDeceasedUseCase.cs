using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;

public sealed class UnverifiedDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IUnverifiedDeceasedUseCase
{
    public Task<Result<UnverifiedDeceasedResponse, Error>> Execute(
        UnverifiedDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<UnverifiedDeceasedResponse, Error>> Handle(
        UnverifiedDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), 
            UserRole.Admin.ToString());
        
        if (!isAdmin)
            return Errors.Deceased.UnverifiedForbidden();
        
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var result = deceased.Unverify();
        if (result.IsFailure)
            return result.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<UnverifiedDeceasedResponse, Error>(
            new UnverifiedDeceasedResponse(deceased.Id, deceased.IsVerified));
    }
}