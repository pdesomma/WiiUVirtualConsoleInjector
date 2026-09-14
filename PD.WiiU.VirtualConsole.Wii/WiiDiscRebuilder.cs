using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Re-lays a plaintext disc's game partition into a fresh single-partition image, optionally with main.dol replaced.
/// </summary>
public static class WiiDiscRebuilder
{
    /// <summary>
    /// Alignment of every file in the rebuilt partition.
    /// </summary>
    public const int FileAlignment = 0x20;

    private const int DolHeaderSize = 0x100;
    private const int DolSections = 18;

    /// <summary>
    /// Rebuilds the first data partition; other partitions are dropped.
    /// </summary>
    /// <param name="plainDisc">Seekable disc whose partition data is plaintext.</param>
    /// <param name="output">Seekable destination.</param>
    /// <param name="patchMainDol">Transform applied to main.dol, or null to keep it.</param>
    /// <param name="cancellationToken">Cancels between hash groups.</param>
    public static WiiDiscBuildResult Rebuild(Stream plainDisc, Stream output, Func<byte[], byte[]>? patchMainDol = null, CancellationToken cancellationToken = default)
    {
        if (plainDisc is null)
            throw new ArgumentNullException(nameof(plainDisc));
        if (output is null)
            throw new ArgumentNullException(nameof(output));

        var disc = WiiDisc.Read(plainDisc);
        if (disc.DataPartitions.Count == 0)
            throw new InvalidDataException("Disc has no data partition.");

        var partition = disc.DataPartitions[0];
        var system = PartitionSystemFiles.Read(plainDisc, partition);
        var data = new PartitionDataStream(plainDisc, partition);
        var boot = system.Boot;
        var dolOffset = (long)ReadUInt32(boot, 0x420) << 2;
        var fstOffset = (long)ReadUInt32(boot, 0x424) << 2;
        var fstSize = (int)(ReadUInt32(boot, 0x428) << 2);

        var dol = ReadAt(data, dolOffset, DolLength(ReadAt(data, dolOffset, DolHeaderSize)));
        if (patchMainDol is not null)
            dol = patchMainDol(dol);

        var builder = new WiiDiscBuilder(disc.Header.GameId, disc.Header.Title, system, dol)
        {
            DiscNumber = disc.Header.DiscNumber,
            Version = disc.Header.Version,
            Region = RegionArea.Read(plainDisc),
            FileAlignment = FileAlignment,
        };
        foreach (var file in Fst.Parse(ReadAt(data, fstOffset, fstSize)))
            builder.Files.Add(new DiscFile(file.Path, new SliceStream(data, file.Offset, file.Length)));
        return builder.Build(output, cancellationToken);
    }

    /// <summary>
    /// Bytes a DOL occupies: the end of its furthest section.
    /// </summary>
    /// <param name="header">First <c>0x100</c> bytes.</param>
    /// <exception cref="InvalidDataException">No section has data.</exception>
    public static int DolLength(byte[] header)
    {
        if (header is null)
            throw new ArgumentNullException(nameof(header));
        if (header.Length < DolHeaderSize)
            throw new ArgumentException($"Need at least {DolHeaderSize} bytes.", nameof(header));

        long end = 0;
        for (var i = 0; i < DolSections; i++)
        {
            var size = ReadUInt32(header, 0x90 + i * 4);
            if (size != 0)
                end = Math.Max(end, ReadUInt32(header, i * 4) + (long)size);
        }
        if (end < DolHeaderSize || end > int.MaxValue)
            throw new InvalidDataException("DOL header describes no sections.");
        return (int)end;
    }

    private static byte[] ReadAt(Stream stream, long position, int count)
    {
        var bytes = new byte[count];
        stream.Position = position;
        var read = 0;
        while (read < count)
        {
            var n = stream.Read(bytes, read, count - read);
            if (n == 0)
                throw new EndOfStreamException("Partition ends inside a system file.");
            read += n;
        }
        return bytes;
    }

    private static uint ReadUInt32(byte[] bytes, int offset) =>
        (uint)(bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3]);
}
