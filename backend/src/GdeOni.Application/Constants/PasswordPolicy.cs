namespace GdeOni.Application.Constants;

public static class PasswordPolicy
{
    public const int MinPasswordLength = 8;

    // BCrypt всё равно режет до 72 байт, 128 — запас под пасс-менеджеры
    // и кириллицу (в UTF-8 один символ занимает до 4 байт). Без верхнего
    // лимита 10MB Password тратит CPU/RAM на heap-allocation + JSON-парсинг
    // непропорционально полезной нагрузке. См. D7.54.
    public const int MaxPasswordLength = 128;
}