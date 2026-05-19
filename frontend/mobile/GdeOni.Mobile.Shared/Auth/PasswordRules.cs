namespace GdeOni.Mobile.Shared.Auth;

/// <summary>
/// Чистые правила валидации формы смены пароля. Не дёргает backend —
/// просто проверяет клиентские инварианты (см. ChangePasswordViewModel).
/// Backend-валидатор всё равно перепроверяет, но клиентский guard
/// экономит круг до сервера на простых ошибках.
/// </summary>
public static class PasswordRules
{
    public static bool IsTooShort(string? newPassword) =>
        !string.IsNullOrEmpty(newPassword) &&
        newPassword.Length < PasswordPolicy.MinPasswordLength;

    public static bool IsTooLong(string? newPassword) =>
        (newPassword?.Length ?? 0) > PasswordPolicy.MaxPasswordLength;

    public static bool PasswordsMatch(string? newPassword, string? confirmPassword) =>
        !string.IsNullOrEmpty(newPassword) &&
        newPassword == confirmPassword;

    /// <summary>
    /// Можно ли отправлять форму: current не пуст, новый в допустимом
    /// диапазоне, и confirm совпадает.
    /// </summary>
    public static bool CanSubmit(string? currentPassword, string? newPassword, string? confirmPassword) =>
        !string.IsNullOrEmpty(currentPassword) &&
        !IsTooShort(newPassword) &&
        !IsTooLong(newPassword) &&
        PasswordsMatch(newPassword, confirmPassword);
}
