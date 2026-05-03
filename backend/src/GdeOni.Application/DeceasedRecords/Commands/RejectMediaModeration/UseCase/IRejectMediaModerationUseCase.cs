using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.UseCase;

public interface IRejectMediaModerationUseCase
{
    Task<UnitResult<Error>> Execute(
        RejectMediaModerationCommand command,
        CancellationToken cancellationToken);
}
