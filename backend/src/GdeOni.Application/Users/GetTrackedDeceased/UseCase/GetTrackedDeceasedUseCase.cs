using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.GetTrackedDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.GetTrackedDeceased.UseCase;

public sealed class GetTrackedDeceasedUseCase(IUserRepository userRepository)
    : IGetTrackedDeceasedUseCase
{
    public async Task<Result<GetTrackedDeceasedResponse, Error>> Execute(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithTracking(userId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", userId);

        var items = user.TrackedDeceasedItems
            .Select(x => new TrackedDeceasedItemResponse
            {
                DeceasedId = x.DeceasedId,
                RelationshipType = x.RelationshipType.ToString(),
                PersonalNotes = x.PersonalNotes,
                NotifyOnDeathAnniversary = x.NotifyOnDeathAnniversary,
                NotifyOnBirthAnniversary = x.NotifyOnBirthAnniversary,
                Status = x.Status.ToString(),
                TrackedAtUtc = x.TrackedAtUtc
            })
            .ToList();

        return Result.Success<GetTrackedDeceasedResponse, Error>(
            new GetTrackedDeceasedResponse { Items = items });
    }
}