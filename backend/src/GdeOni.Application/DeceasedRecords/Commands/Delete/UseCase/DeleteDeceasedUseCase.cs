using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Delete.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;

public sealed class DeleteDeceasedUseCase(
    IDeceasedRepository deceasedRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IDeleteDeceasedUseCase
{
    public Task<Result<DeleteDeceasedResponse, Error>> Execute(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(command, Handle, cancellationToken);
    }

    private async Task<Result<DeleteDeceasedResponse, Error>> Handle(
        DeleteDeceasedCommand command,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole(UserRole.SuperAdmin.ToString(), 
            UserRole.Admin.ToString());
        
        if (!isAdmin)
            return Errors.Deceased.DeleteForbidden();
        
        var deceased = await deceasedRepository.GetById(command.Id, cancellationToken);
        if (deceased is null)
            return Errors.General.NotFound("deceased", command.Id);

        deceasedRepository.Delete(deceased);
        await deceasedRepository.Save(cancellationToken);

        return Result.Success<DeleteDeceasedResponse, Error>(
            new DeleteDeceasedResponse(true));
    }
}