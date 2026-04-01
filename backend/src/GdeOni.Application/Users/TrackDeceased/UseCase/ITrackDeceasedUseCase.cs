using CSharpFunctionalExtensions;
using GdeOni.Application.Users.TrackDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.TrackDeceased.UseCase;

public interface ITrackDeceasedUseCase
{
    Task<Result<TrackDeceasedResponse, Error>> Execute(
        TrackDeceasedRequest request,
        CancellationToken cancellationToken);
}