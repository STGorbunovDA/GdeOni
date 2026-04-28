using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetTrackedDeceased.UseCase;

public sealed class GetTrackedDeceasedUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService)
    : IGetTrackedDeceasedUseCase
{
    public async Task<Result<GetTrackedDeceasedResponse, Error>> Execute(
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;

        var user = await userRepository.GetByIdWithTracking(currentUserId, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserId);

        var items = user.TrackedDeceasedItems
            .Select(x => new TrackedDeceasedItemResponse(
                Id: x.Id,
                UserId: currentUserId,
                DeceasedId: x.DeceasedId,
                RelationshipType: x.RelationshipType.ToString(),
                PersonalNotes: x.PersonalNotes,
                NotifyOnDeathAnniversary: x.NotifyOnDeathAnniversary,
                NotifyOnBirthAnniversary: x.NotifyOnBirthAnniversary,
                HasNotificationsEnabled: x.HasNotificationsEnabled(),
                Status: x.Status.ToString(),
                TrackedAtUtc: x.TrackedAtUtc,
                IsActive: x.IsActive(),
                IsMuted: x.IsMuted(),
                IsArchived: x.IsArchived()))
            .ToList();

        return Result.Success<GetTrackedDeceasedResponse, Error>(
            new GetTrackedDeceasedResponse(items));
    }
}