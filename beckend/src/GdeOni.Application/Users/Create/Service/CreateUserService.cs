using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Users.Create.Service;

public sealed class CreateUserService : ICreateUserService
{
    private readonly IUserRepository _userRepository;

    public CreateUserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<CreateUserResponse>> ExecuteAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Result.Failure<CreateUserResponse>("Request обязателен");

        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure<CreateUserResponse>("Email обязателен");

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
            return Result.Failure<CreateUserResponse>("PasswordHash обязателен");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var finalUserName = string.IsNullOrWhiteSpace(request.UserName)
            ? normalizedEmail.Split('@')[0]
            : request.UserName.Trim();

        var emailExists = await _userRepository.ExistsByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (emailExists)
            return Result.Failure<CreateUserResponse>("Пользователь с таким email уже существует");

        var userNameExists = await _userRepository.ExistsByUserNameAsync(
            finalUserName,
            cancellationToken);

        if (userNameExists)
            return Result.Failure<CreateUserResponse>("Пользователь с таким userName уже существует");

        var userResult = User.Register(
            normalizedEmail,
            request.PasswordHash,
            request.FullName,
            request.UserName);

        if (userResult.IsFailure)
            return Result.Failure<CreateUserResponse>(userResult.Error);

        var user = userResult.Value;

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateUserResponse(user.Id));
    }
}