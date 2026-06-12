namespace GdeOni.Application.Abstractions.Storage;

/// <summary>
/// D33. Возвращает имя bucket'а MinIO для вложений в обращения
/// в поддержку. Сделано отдельной абстракцией, чтобы Application
/// не знал про MinioOptions из Infrastructure.
/// </summary>
public interface ISupportAttachmentsBucketResolver
{
    string GetBucket();
}
