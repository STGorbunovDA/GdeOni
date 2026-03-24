using GdeOni.Domain.Shared;

namespace GdeOni.API.Response;

public sealed class ApiResponse<T>
{
    public T? Result { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyCollection<ValidationErrorDetail>? Errors { get; }
    public DateTime TimeGenerated { get; }

    private ApiResponse(
        T? result,
        string? errorCode,
        string? errorMessage,
        IReadOnlyCollection<ValidationErrorDetail>? errors,
        DateTime timeGenerated)
    {
        Result = result;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Errors = errors;
        TimeGenerated = timeGenerated;
    }

    public static ApiResponse<T> Success(T result) =>
        new(result, null, null, null, DateTime.UtcNow);

    public static ApiResponse<T> Error(Error error) =>
        new(default, error.Code, error.Message, error.Details, DateTime.UtcNow);
}