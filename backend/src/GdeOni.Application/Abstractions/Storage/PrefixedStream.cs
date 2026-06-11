namespace GdeOni.Application.Abstractions.Storage;

/// <summary>
/// Stream, который сначала отдаёт байты префикса (например, прочитанные
/// при magic-bytes валидации), а затем переходит к чтению из основного
/// потока. Используется чтобы не делать Seek(0) на исходном Stream —
/// благодаря этому upload pipeline работает с non-seekable streams
/// (request body, NetworkStream и т.п.). Read-only, не поддерживает
/// Seek / Position / Length / Write.
/// </summary>
internal sealed class PrefixedStream(byte[] prefix, Stream inner) : Stream
{
    private readonly byte[] _prefix = prefix;
    private readonly Stream _inner = inner;
    private int _prefixPosition;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var fromPrefix = ReadFromPrefix(buffer.AsSpan(offset, count));
        if (fromPrefix > 0)
            return fromPrefix;

        return _inner.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var fromPrefix = ReadFromPrefix(buffer.Span);
        if (fromPrefix > 0)
            return fromPrefix;

        return await _inner.ReadAsync(buffer, cancellationToken);
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        var fromPrefix = ReadFromPrefix(buffer.AsSpan(offset, count));
        if (fromPrefix > 0)
            return Task.FromResult(fromPrefix);

        return _inner.ReadAsync(buffer, offset, count, cancellationToken);
    }

    private int ReadFromPrefix(Span<byte> destination)
    {
        var remaining = _prefix.Length - _prefixPosition;
        if (remaining <= 0)
            return 0;

        var toCopy = Math.Min(remaining, destination.Length);
        _prefix.AsSpan(_prefixPosition, toCopy).CopyTo(destination);
        _prefixPosition += toCopy;
        return toCopy;
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        await base.DisposeAsync();
    }
}
