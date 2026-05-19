using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

public interface IDeceasedMemoriesApi
{
    /// <summary>
    /// Создать воспоминание. Backend кладёт новую запись в ModerationStatus.Pending —
    /// автор карточки и админ её сразу видят (canSeeAllMemories=true в
    /// GetMyTrackedDeceasedDetails), всем остальным запись появится только
    /// после одобрения.
    /// </summary>
    [Post("/api/deceased-records/{deceasedId}/memories")]
    Task<ApiEnvelope<AddMemoryResponse>> AddAsync(
        Guid deceasedId,
        [Body] AddMemoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Полное обновление текста (PUT, не PATCH). Backend после редактирования
    /// возвращает запись в Pending — потребуется повторная модерация (D7.22).
    /// </summary>
    [Put("/api/deceased-records/{deceasedId}/memories/{memoryId}")]
    Task<ApiEnvelope<UpdateMemoryResponse>> UpdateAsync(
        Guid deceasedId,
        Guid memoryId,
        [Body] UpdateMemoryRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/deceased-records/{deceasedId}/memories/{memoryId}")]
    Task<HttpResponseMessage> DeleteAsync(
        Guid deceasedId,
        Guid memoryId,
        CancellationToken cancellationToken = default);
}
