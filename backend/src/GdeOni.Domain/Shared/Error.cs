namespace GdeOni.Domain.Shared;

public class Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }
    public IReadOnlyCollection<ValidationErrorDetail> Details { get; }

    private Error(
        string code,
        string message,
        ErrorType type,
        IReadOnlyCollection<ValidationErrorDetail>? details = null)
    {
        Code = code;
        Message = message;
        Type = type;
        Details = details ?? Array.Empty<ValidationErrorDetail>();
    }

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Validation(
        string code,
        string message,
        IReadOnlyCollection<ValidationErrorDetail> details) =>
        new(code, message, ErrorType.Validation, details);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);
}

public sealed record ValidationErrorDetail(
    string PropertyName,
    string ErrorMessage);

public enum ErrorType
{
    Validation,
    NotFound,
    Failure,
    Conflict
}