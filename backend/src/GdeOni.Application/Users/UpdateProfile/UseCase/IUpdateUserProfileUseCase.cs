using CSharpFunctionalExtensions;
using GdeOni.Application.Users.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.UpdateProfile.UseCase;

public interface IUpdateUserProfileUseCase
{
    Task<Result<UpdateUserProfileResponse, Error>> Execute(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken);
}