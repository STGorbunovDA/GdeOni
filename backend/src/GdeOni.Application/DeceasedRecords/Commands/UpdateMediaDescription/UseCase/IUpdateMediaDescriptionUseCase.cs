using CSharpFunctionalExtensions;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.UseCase;

public interface IUpdateMediaDescriptionUseCase
{
    Task<Result<UpdateMediaDescriptionResponse, Error>> Execute(
        UpdateMediaDescriptionCommand command,
        CancellationToken cancellationToken);
}
