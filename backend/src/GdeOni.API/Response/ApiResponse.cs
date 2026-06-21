using GdeOni.Domain.Shared;

namespace GdeOni.API.Response;

/// <summary>
/// Унифицированный конверт ответа API: успешный <typeparamref name="T"/>
/// в <see cref="Result"/> или подробности ошибки в <see cref="ErrorCode"/>/
/// <see cref="ErrorMessage"/>/<see cref="Errors"/>.
/// </summary>
/// <typeparam name="T">Тип полезной нагрузки успешного ответа.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Полезная нагрузка успешного ответа (null при ошибке).</summary>
    public T? Result { get; }
    /// <summary>Код ошибки в формате <c>{entity}.{field}.{rule}</c> (null при успехе).</summary>
    public string? ErrorCode { get; }
    /// <summary>Человекочитаемое сообщение об ошибке (null при успехе).</summary>
    public string? ErrorMessage { get; }
    /// <summary>Список деталей валидации (null если ошибка не валидационная).</summary>
    public IReadOnlyCollection<ValidationErrorDetail>? Errors { get; }
    /// <summary>Момент формирования ответа в UTC.</summary>
    public DateTimeOffset TimeGenerated { get; }

    private ApiResponse(
        T? result,
        string? errorCode,
        string? errorMessage,
        IReadOnlyCollection<ValidationErrorDetail>? errors,
        DateTimeOffset timeGenerated)
    {
        Result = result;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Errors = errors;
        TimeGenerated = timeGenerated;
    }

    /// <summary>Создаёт успешный ответ с полезной нагрузкой <paramref name="result"/>.</summary>
    public static ApiResponse<T> Success(T result) =>
        new(result, null, null, null, DateTimeOffset.UtcNow);

    /// <summary>Создаёт ответ-ошибку из доменного <paramref name="error"/>.</summary>
    public static ApiResponse<T> Error(Error error) =>
        new(default, error.Code, error.Message, error.Details, DateTimeOffset.UtcNow);
}
