namespace GdeOni.Application.Abstractions.Storage;

public interface IFileStorage
{
    Task<StoredFile> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    string GetPublicUrl(string bucket, string objectKey);

    Task<string> GetPresignedUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken);
}
