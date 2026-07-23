using CSharpFunctionalExtensions;
using GdeOni.Application.Auth.ForgotPassword.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Auth.ForgotPassword.UseCase;

public interface IForgotPasswordUseCase
{
    /// <summary>
    /// D43. Запрашивает ссылку восстановления. Ошибку возвращает ТОЛЬКО
    /// на невалидную форму email — существует ли такой пользователь,
    /// наружу не сообщается (см. реализацию).
    /// </summary>
    Task<UnitResult<Error>> Execute(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken);
}
