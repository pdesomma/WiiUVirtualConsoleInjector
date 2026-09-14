namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Read-only window onto a range of a seekable stream; repositions the source on every read.
/// </summary>
internal sealed class SliceStream : Stream
{
    private readonly long _length;
    private readonly Stream _source;
    private readonly long _start;
    private long _position;

    public SliceStream(Stream source, long start, long length)
    {
        _source = source;
        _start = start;
        _length = length;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get => _position;
        set => _position = value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = _length - _position;
        if (remaining <= 0)
            return 0;

        _source.Position = _start + _position;
        var read = _source.Read(buffer, offset, (int)Math.Min(count, remaining));
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        return _position;
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
