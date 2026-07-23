using GdeOni.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace GdeOni.Infrastructure.Storage;

internal sealed class SupportAttachmentsBucketResolver(IOptions<MinioOptions> options)
    : ISupportAttachmentsBucketResolver
{
    public string GetBucket() => options.Value.Buckets.SupportAttachments;
}
