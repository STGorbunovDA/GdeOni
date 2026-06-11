namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// Backend-обёртка ответа: { result, errorCode, errorMessage, errors, timeGenerated }.
/// Назван ApiEnvelope (не ApiResponse), чтобы не конфликтовать с Refit.ApiResponse.
/// </summary>
public sealed record ApiEnvelope<T>(
    T? Result,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<ValidationErrorDetail>? Errors,
    DateTimeOffset TimeGenerated);

public sealed record ValidationErrorDetail(
    string PropertyName,
    string ErrorCode,
    string ErrorMessage);
