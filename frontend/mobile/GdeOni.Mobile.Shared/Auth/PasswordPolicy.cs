namespace GdeOni.Mobile.Shared.Auth;

/// <summary>
/// Ограничения длины пароля. Синхронизированы с backend
/// (GdeOni.Application.Constants — D11.x). Изменение значений здесь
/// должно сопровождаться правкой backend-валидатора и наоборот.
/// </summary>
public static class PasswordPolicy
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
}
