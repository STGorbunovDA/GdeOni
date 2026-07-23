using GdeOni.Application.Abstractions.Email;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Auth.ForgotPassword.Model;
using GdeOni.Application.Auth.ForgotPassword.UseCase;
using GdeOni.Application.Auth.ForgotPassword.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Tests.TestSupport;
using GdeOni.Domain.Aggregates.User;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GdeOni.Application.Tests.Auth;

/// <summary>
/// D43. Тесты <see cref="ForgotPasswordUseCase"/>.
///
/// Главный риск здесь — user enumeration: по ответу эндпоинта не должно
/// быть видно, зарегистрирован адрес или нет. Для сервиса про умерших
/// родственников сам факт «этот человек здесь есть» чувствителен,
/// поэтому неразглашение проверяем отдельными тестами.
/// </summary>
public sealed class ForgotPasswordUseCaseTests
{
    private const string KnownEmail = "ivan@example.com";
    private const string PasswordHash = "hash$with$enough$chars";

    private static readonly PasswordResetOptions DefaultOptions = new()
    {
        TokenLifetimeMinutes = 60,
        WebResetUrl = "https://gdeoni.ru/reset-password",
        AppName = "Где Они",
    };

    [Fact]
    public async Task Execute_KnownEmail_StoresTokenAndSendsLetter()
    {
        var user = CreateUser();
        var (useCase, repo, sender) = Build(user);

        var result = await useCase.Execute(new ForgotPasswordCommand(KnownEmail), default);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetTokenHash.Should().NotBeNullOrWhiteSpace();
        user.PasswordResetTokenExpiresAtUtc.Should().NotBeNull();
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Неизвестный адрес → тот же успех. Иначе эндпоинт превращается
    /// в перечислитель зарегистрированных пользователей.
    /// </summary>
    [Fact]
    public async Task Execute_UnknownEmail_ReturnsSuccessAndSendsNothing()
    {
        var (useCase, _, sender) = Build(user: null);

        var result = await useCase.Execute(
            new ForgotPasswordCommand("nobody@example.com"), default);

        result.IsSuccess.Should().BeTrue();
        sender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Заблокированному сброс пароля доступ не вернёт — письмо не шлём,
    /// но наружу разницы по-прежнему не видно.
    /// </summary>
    [Fact]
    public async Task Execute_BlockedUser_ReturnsSuccessAndSendsNothing()
    {
        var user = CreateUser();
        user.Block(Guid.NewGuid(), "спам", DateTime.UtcNow);
        var (useCase, _, sender) = Build(user);

        var result = await useCase.Execute(new ForgotPasswordCommand(KnownEmail), default);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetTokenHash.Should().BeNull();
        sender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Почтовый канал выключен — токен не выписываем вовсе: ссылку всё
    /// равно нечем доставить, а мёртвая запись в БД только путает.
    /// </summary>
    [Fact]
    public async Task Execute_EmailChannelDisabled_ReturnsSuccessWithoutToken()
    {
        var user = CreateUser();
        var (useCase, repo, sender) = Build(user, emailEnabled: false);

        var result = await useCase.Execute(new ForgotPasswordCommand(KnownEmail), default);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetTokenHash.Should().BeNull();
        repo.Verify(x => x.Save(It.IsAny<CancellationToken>()), Times.Never);
        sender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Адрес страницы сброса не настроен — ссылка получилась бы битой.
    /// Ведём себя как при выключенном канале.
    /// </summary>
    [Fact]
    public async Task Execute_ResetUrlNotConfigured_ReturnsSuccessWithoutToken()
    {
        var user = CreateUser();
        var options = new PasswordResetOptions { WebResetUrl = string.Empty };
        var (useCase, _, sender) = Build(user, options: options);

        var result = await useCase.Execute(new ForgotPasswordCommand(KnownEmail), default);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetTokenHash.Should().BeNull();
        sender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// SMTP упал — наружу всё равно успех (иначе enumeration через
    /// разницу в ответе), но токен уже сохранён и в логе будет ошибка.
    /// </summary>
    [Fact]
    public async Task Execute_SendThrows_StillReturnsSuccess()
    {
        var user = CreateUser();
        var (useCase, _, sender) = Build(user);
        sender
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await useCase.Execute(new ForgotPasswordCommand(KnownEmail), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_InvalidEmailForm_ReturnsValidationError()
    {
        var (useCase, _, _) = Build(user: null);

        var result = await useCase.Execute(new ForgotPasswordCommand("not-an-email"), default);

        result.IsFailure.Should().BeTrue();
        // ValidatedUseCaseExecutor заворачивает ошибки FluentValidation
        // в общий конверт; конкретный код лежит в Details[].
        result.Error.Code.Should().Be("validation.failed");
        result.Error.Details.Should().Contain(e => e.ErrorCode == "user.email.invalid");
    }

    /// <summary>Письмо должно содержать рабочую ссылку с токеном.</summary>
    [Fact]
    public async Task Execute_SentLetter_ContainsResetLink()
    {
        var user = CreateUser();
        EmailMessage? captured = null;
        var (useCase, _, sender) = Build(user);
        sender
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        await useCase.Execute(new ForgotPasswordCommand(KnownEmail), default);

        captured.Should().NotBeNull();
        captured!.ToEmail.Should().Be(KnownEmail);
        captured.TextBody.Should().Contain("https://gdeoni.ru/reset-password?token=");
    }

    private static (IForgotPasswordUseCase UseCase, Mock<IUserRepository> Repo, Mock<IEmailSender> Sender)
        Build(User? user, bool emailEnabled = true, PasswordResetOptions? options = null)
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(x => x.GetByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repo.Setup(x => x.Save(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sender = new Mock<IEmailSender>();
        sender.SetupGet(x => x.IsEnabled).Returns(emailEnabled);
        sender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tokenFactory = new Mock<ISecureTokenFactory>();
        tokenFactory.Setup(x => x.Generate()).Returns("plain-token");
        tokenFactory.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(t => $"hash::{t}");

        var useCase = new ForgotPasswordUseCase(
            repo.Object,
            tokenFactory.Object,
            sender.Object,
            Microsoft.Extensions.Options.Options.Create(options ?? DefaultOptions),
            TestExecutor.With<ForgotPasswordCommand, ForgotPasswordCommandValidator>(),
            NullLogger<ForgotPasswordUseCase>.Instance,
            TimeProvider.System);

        return (useCase, repo, sender);
    }

    private static User CreateUser() =>
        User.Register(email: KnownEmail, passwordHash: PasswordHash).Value;
}
