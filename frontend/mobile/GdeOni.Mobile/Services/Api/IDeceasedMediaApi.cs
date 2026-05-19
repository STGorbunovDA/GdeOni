using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.Services.Api;

public interface IDeceasedMediaApi
{
    [Multipart]
    [Post("/api/deceased-records/{deceasedId}/media")]
    Task<ApiEnvelope<UploadMediaResponse>> UploadAsync(
        Guid deceasedId,
        [AliasAs("file")] StreamPart file,
        [AliasAs("kind")] int kind,
        [AliasAs("description")] string? description,
        CancellationToken cancellationToken = default);

    [Get("/api/deceased-records/{deceasedId}/media")]
    Task<ApiEnvelope<PagedResponse<MediaListItem>>> GetListAsync(
        Guid deceasedId,
        [Query] int page = 1,
        [Query] int pageSize = 50,
        CancellationToken cancellationToken = default);

    [Delete("/api/deceased-records/{deceasedId}/media/{mediaId}")]
    Task<HttpResponseMessage> DeleteAsync(
        Guid deceasedId,
        Guid mediaId,
        CancellationToken cancellationToken = default);

    [Patch("/api/deceased-records/{deceasedId}/media/{mediaId}/main-photo")]
    Task<HttpResponseMessage> SetMainPhotoAsync(
        Guid deceasedId,
        Guid mediaId,
        CancellationToken cancellationToken = default);
}
