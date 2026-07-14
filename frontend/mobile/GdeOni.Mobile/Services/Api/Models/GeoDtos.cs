namespace GdeOni.Mobile.Services.Api.Models;

/// <summary>
/// D41. Ответ обратного геокодирования. Любое поле может быть null:
/// посреди леса города нет.
/// </summary>
public sealed record ReverseGeocodeResponse(
    string? Country,
    string? Region,
    string? City);
