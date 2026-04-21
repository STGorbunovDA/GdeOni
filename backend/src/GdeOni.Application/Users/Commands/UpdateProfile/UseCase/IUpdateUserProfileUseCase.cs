using CSharpFunctionalExtensions;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Commands.UpdateProfile.UseCase;

public interface IUpdateUserProfileUseCase
{
    Task<Result<UpdateUserProfileResponse, Error>> Execute(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken);
}