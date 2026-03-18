using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Create.UseCase;

public sealed class CreateUserUseCase : ICreateUserUseCase
{
    private readonly IUserRepository _userRepository;

    public CreateUserUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<CreateUserResponse, Error>> Execute(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Errors.General.ValueIsRequired(nameof(CreateUserRequest));

        if (string.IsNullOrWhiteSpace(request.Email))
            return Errors.User.EmailRequired();

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
            return Errors.User.PasswordHashRequired();

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var finalUserName = string.IsNullOrWhiteSpace(request.UserName)
            ? normalizedEmail.Split('@')[0]
            : request.UserName.Trim();

        var emailExists = await _userRepository.ExistsByEmail(
            normalizedEmail,
            cancellationToken);

        if (emailExists)
            return Error.Conflict(
                "user.email.already.exists",
                "User with this email already exists");

        var userNameExists = await _userRepository.ExistsByUserName(
            finalUserName,
            cancellationToken);

        if (userNameExists)
            return Error.Conflict(
                "user.user_name.already.exists",
                "User with this user name already exists");

        var userResult = User.Register(
            normalizedEmail,
            request.PasswordHash,
            request.FullName,
            request.UserName);

        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;

        await _userRepository.Add(user, cancellationToken);
        await _userRepository.Save(cancellationToken);

        return Result.Success<CreateUserResponse, Error>(
            new CreateUserResponse(user.Id));
    }
}