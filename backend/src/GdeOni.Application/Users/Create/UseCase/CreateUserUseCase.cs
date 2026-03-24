using CSharpFunctionalExtensions;
using FluentValidation;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Application.Validation;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Create.UseCase;

public sealed class CreateUserUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IValidator<CreateUserRequest> validator)
    : ICreateUserUseCase
{
    public async Task<Result<CreateUserResponse, Error>> Execute(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Errors.General.ValueIsRequired(nameof(CreateUserRequest));

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToValidationError();

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

        var passwordHash = passwordHasher.Hash(request.Password);

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
        catch (UniqueConstraintException ex) when (ex.ConstraintName == DbConstraints.UxUsersEmail)
        {
            return Errors.User.EmailAlreadyExists();
        }

        return Result.Success<CreateUserResponse, Error>(
            new CreateUserResponse(user.Id));
    }
}