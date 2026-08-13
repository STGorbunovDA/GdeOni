using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Auth.ConfirmEmail;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Application.Users.Commands.Register.UseCase;
using GdeOni.Application.Users.Commands.Register.Validation;
using GdeOni.Domain.Aggregates.User;

namespace GdeOni.Application.Tests.Users;

/// <summary>
/// Тесты <see cref="RegisterUserUseCase"/>: уникальность email/userName,
/// хеширование пароля, корректное создание User через домен-фабрику,
/// D19 возрастной гард (14 лет).
/// </summary>
public sealed class RegisterUserUseCaseTests
{
    /// <summary>Дата рождения, гарантированно проходящая гард 14 лет.</summary>
    private static readonly DateOnly AdultBirthDate = new(2000, 1, 1);

    [Fact]
    public async Task Execute_DuplicateEmail_ReturnsEmailAlreadyExists()
    {
        var (userRepo, _, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "john@example.com", null, null, "Password123!",
                AdultBirthDate, true, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.email.already.exists");
        userRepo.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Тест на дубль UserName удалён: имя пользователя больше НЕ уникально
    // (тёзки допустимы, вход по email) — проверка уникальности снята
    // из RegisterUserUseCase вместе с индексом (2026-07-26).

    [Fact]
    public async Task Execute_HappyPath_AddsUserAndSaves()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hasher.Setup(x => x.Hash("Password123!")).Returns("hash-bcrypt");

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "new@example.com", "newuser", "Иван Иванов", "Password123!",
                AdultBirthDate, true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
        hasher.Verify(x => x.Hash("Password123!"), Times.Once);
        userRepo.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        userRepo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Логин по умолчанию — часть email до «@».
    /// </summary>
    [Fact]
    public async Task Execute_AssignsLoginFromEmailPrefix()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        // Ни один логин не занят.
        userRepo
            .Setup(x => x.ExistsByLogin(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash-bcrypt");

        User? added = null;
        userRepo
            .Setup(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u);

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "bous07@mail.ru", null, null, "Password123!",
                AdultBirthDate, true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added!.Login.Should().Be("bous07");
    }

    /// <summary>
    /// Префикс email уже занят («bous07» от bous07@mail.ru), регистрируется
    /// bous07@yandex.ru → логином становится ПОЛНЫЙ адрес, а не «bous072».
    /// Регистрация при этом проходит: email-то свободен.
    /// </summary>
    [Fact]
    public async Task Execute_PrefixTaken_AssignsFullEmailAsLogin()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        // «bous07» занят прежним пользователем, полный адрес — свободен.
        userRepo
            .Setup(x => x.ExistsByLogin("bous07", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userRepo
            .Setup(x => x.ExistsByLogin("bous07@yandex.ru", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash-bcrypt");

        User? added = null;
        userRepo
            .Setup(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u);

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "bous07@yandex.ru", null, null, "Password123!",
                AdultBirthDate, true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added!.Login.Should().Be("bous07@yandex.ru");
    }

    /// <summary>
    /// D19. Регистрация младше 14 лет → user.birth_date.min_age. Валидатор
    /// пропускает (BirthDate не пустая), домен отбивает.
    /// </summary>
    [Fact]
    public async Task Execute_UnderMinAge_ReturnsMinAgeNotMet()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");

        // Ребёнок, которому "сейчас" 10 лет: birthDate = today - 10 years.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tenYearsOld = today.AddYears(-10);

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "kid@example.com", null, null, "Password123!",
                tenYearsOld, true, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.birth_date.min_age");
        userRepo.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// D19. Пустая (default) BirthDate → валидатор режет как «required»
    /// раньше, чем домен получит команду.
    /// </summary>
    [Fact]
    public async Task Execute_DefaultBirthDate_ReturnsRequired()
    {
        var (userRepo, _, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "empty@example.com", null, null, "Password123!",
                default, true, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        // ValidatedUseCaseExecutor заворачивает FluentValidation-ошибки в
        // общий "validation.failed"; конкретный код лежит в Details[].ErrorCode
        // через маппинг в ValidationExtensions.
        result.Error.Code.Should().Be("validation.failed");
        result.Error.Details.Should()
            .Contain(e => e.ErrorCode == "user.birth_date.required");
    }

    /// <summary>
    /// D19. Ровно 14-летний юзер (день рождения сегодня) — регистрируется.
    /// </summary>
    [Fact]
    public async Task Execute_ExactlyMinAge_Succeeds()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");

        // День рождения ровно 14 лет назад.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var exactly14 = today.AddYears(-14);

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "teen@example.com", null, null, "Password123!",
                exactly14, true, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepo.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// D19. Дата рождения в будущем → user.birth_date.invalid (домен).
    /// </summary>
    [Fact]
    public async Task Execute_FutureBirthDate_ReturnsInvalid()
    {
        var (userRepo, hasher, useCase) = BuildHarness();
        userRepo
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userRepo
            .Setup(x => x.ExistsByUserName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");

        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);

        var result = await useCase.Execute(
            new RegisterUserCommand(
                "future@example.com", null, null, "Password123!",
                future, true, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.birth_date.invalid");
    }

    private static (
        Mock<IUserRepository> UserRepo,
        Mock<IPasswordHasher> Hasher,
        RegisterUserUseCase UseCase) BuildHarness()
    {
        var userRepo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        // D16: RegisterUserUseCase дёргает StartTrial и читает
        // длительность из SubscriptionOptions через IOptions.
        var subscriptionOptions = Microsoft.Extensions.Options.Options.Create(
            new GdeOni.Application.Subscriptions.SubscriptionOptions());
        // D19: RegisterUserUseCase также читает текущие версии Privacy/Terms
        // из LegalOptions и вызывает User.AcceptLegal.
        var legalOptions = Microsoft.Extensions.Options.Options.Create(
            new GdeOni.Application.Legal.LegalOptions());
        // D45. По умолчанию IssueConfirmation → null (Mock), письмо не
        // шлётся, гейт не применяется — регистрация в тестах как раньше.
        var emailConfirmation = new Mock<IEmailConfirmationService>();
        var useCase = new RegisterUserUseCase(
            userRepo.Object,
            hasher.Object,
            emailConfirmation.Object,
            TestExecutor.With<RegisterUserCommand, RegisterUserCommandValidator>(),
            subscriptionOptions,
            legalOptions,
            TimeProvider.System);
        return (userRepo, hasher, useCase);
    }
}
