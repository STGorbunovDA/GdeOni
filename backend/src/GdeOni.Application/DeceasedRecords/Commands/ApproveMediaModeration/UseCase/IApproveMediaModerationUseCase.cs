using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.UseCase;

public interface IApproveMediaModerationUseCase
{
    Task<UnitResult<Error>> Execute(
        ApproveMediaModerationCommand command,
        CancellationToken cancellationToken);
}
