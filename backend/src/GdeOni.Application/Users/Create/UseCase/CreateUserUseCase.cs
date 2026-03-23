using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Constants;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Create.UseCase;

public sealed class CreateUserUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    : ICreateUserUseCase
{
    public async Task<Result<CreateUserResponse, Error>> Execute(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Errors.General.ValueIsRequired(nameof(CreateUserRequest));

        if (string.IsNullOrWhiteSpace(request.Email))
            return Errors.User.EmailRequired();

        if (string.IsNullOrWhiteSpace(request.Password))
            return Errors.User.PasswordRequired();
        
        if (request.Password.Length < PasswordPolicy.MIN_PASSWORD_LENGTH)
            return Errors.User.PasswordTooShort();
        
        var passwordHash = passwordHasher.Hash(request.Password);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var finalUserName = string.IsNullOrWhiteSpace(request.UserName)
            ? normalizedEmail.Split('@')[0]
            : request.UserName.Trim();

        var emailExists = await userRepository.ExistsByEmail(
            normalizedEmail,
            cancellationToken);

        if (emailExists)
            return Errors.User.EmailAlreadyExists();

        var userNameExists = await userRepository.ExistsByUserName(
            finalUserName,
            cancellationToken);

        if (userNameExists)
            return Errors.User.UserNameAlreadyExists();

        var userResult = User.Register(
            normalizedEmail,
            passwordHash,
            request.FullName,
            finalUserName);

        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.Value;

        try
        {
            await userRepository.Add(user, cancellationToken);
            await userRepository.Save(cancellationToken);
        }
        catch (UniqueConstraintException ex) when ( ex.ConstraintName == DbConstraints.UxUsersEmail)
        {
            return Errors.User.EmailAlreadyExists();
        }
        catch (UniqueConstraintException ex) when (ex.ConstraintName == DbConstraints.UxUsersName)
        {
            return Errors.User.UserNameAlreadyExists();
        }
        return Result.Success<CreateUserResponse, Error>(
            new CreateUserResponse(user.Id));
    }
}