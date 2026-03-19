using GdeOni.Domain.Shared;

namespace GdeOni.API.Response;

public sealed class ApiResponse<T>
{
    public T? Result { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public DateTime TimeGenerated { get; }

    private ApiResponse(T? result, Error? error)
    {
        Result = result;
        ErrorCode = error?.Code;
        ErrorMessage = error?.Message;
        TimeGenerated = DateTime.UtcNow;
    }

    public static ApiResponse<T> Ok(T? result = default) =>
        new(result, null);

    public static ApiResponse<T> Error(Error error) =>
        new(default, error);
}