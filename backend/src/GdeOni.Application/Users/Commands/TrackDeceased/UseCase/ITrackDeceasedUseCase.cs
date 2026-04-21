using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.TrackDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.TrackDeceased.UseCase;

public interface ITrackDeceasedUseCase
{
    Task<Result<TrackDeceasedResponse, Error>> Execute(
        TrackDeceasedCommand command,
        CancellationToken cancellationToken);
}