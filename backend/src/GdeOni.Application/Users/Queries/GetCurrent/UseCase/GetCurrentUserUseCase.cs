using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Legal;
using GdeOni.Application.Users.Queries.GetCurrent.Model;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Users.Queries.GetCurrent.UseCase;

public sealed class GetCurrentUserUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IOptions<LegalOptions> legalOptions)
    : IGetCurrentUserUseCase
{
    public async Task<Result<GetCurrentUserResponse, Error>> Execute(CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var user = await userRepository.GetByIdReadOnly(currentUserIdResult.Value, cancellationToken);
        if (user is null)
            return Errors.General.NotFound("user", currentUserIdResult.Value);

        var legal = legalOptions.Value;
        return Result.Success<GetCurrentUserResponse, Error>(new GetCurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            PrivacyPolicyVersion = user.PrivacyPolicyVersion,
            TermsVersion = user.TermsVersion,
            HasOutdatedLegalAcceptance = user.HasOutdatedLegalAcceptance(
                legal.CurrentPrivacyPolicyVersion,
                legal.CurrentTermsVersion),
        });
    }
}
