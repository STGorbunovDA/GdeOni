namespace GdeOni.Infrastructure.Storage;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public bool UseSsl { get; set; }

    /// <summary>
    /// Базовый URL, по которому фото отдаются клиенту (например, через nginx).
    /// Если не задан — собирается из Endpoint и UseSsl.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public MinioBucketsOptions Buckets { get; set; } = new();
}

public sealed class MinioBucketsOptions
{
    public string DeceasedPhotos { get; set; } = "deceased-photos";
    public string GravePhotos { get; set; } = "grave-photos";
    public string DeceasedDocuments { get; set; } = "deceased-documents";
}
