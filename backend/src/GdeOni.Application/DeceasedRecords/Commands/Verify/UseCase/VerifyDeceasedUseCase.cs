using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Verify.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Verify.UseCase;

public sealed class VerifyDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IVerifyDeceasedUseCase
{
    public Task<Result<VerifyDeceasedResponse, Error>> Execute(
        VerifyDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<VerifyDeceasedResponse, Error>> Handle(
        VerifyDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), 
            UserRole.Admin.ToString());
        
        if (!isAdmin)
            return Errors.Deceased.VerifyForbidden();
        
        var deceased = await deceasedRepository.GetById(command.DeceasedId, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.DeceasedId);

        var verifyResult = deceased.Verify();
        if (verifyResult.IsFailure)
            return verifyResult.Error;

        await deceasedRepository.Save(cancellationToken);

        return Result.Success<VerifyDeceasedResponse, Error>(
            new VerifyDeceasedResponse(deceased.Id, deceased.IsVerified));
    }
}