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

    /// <summary>
    /// Rebuilds the first data partition; other partitions are dropped. An NKit image is read as is: its partition is plaintext and stored without hash blocks.
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
        var hashed = !IsNkit(plainDisc);
        var system = PartitionSystemFiles.Read(plainDisc, partition, hashed);
        var data = new PartitionDataStream(plainDisc, partition, hashed);
        var boot = system.Boot;
        var dolOffset = (long)ReadUInt32(boot, 0x420) << 2;
        var fstOffset = (long)ReadUInt32(boot, 0x424) << 2;
        var fstSize = (int)(ReadUInt32(boot, 0x428) << 2);

        var dol = ReadAt(data, dolOffset, checked((int)DolHeader.Parse(ReadAt(data, dolOffset, DolHeader.Size)).Length));
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
    /// True when the image carries NKit's block at 0x200.
    /// </summary>
    /// <param name="disc">Seekable disc image.</param>
    public static bool IsNkit(Stream disc)
    {
        if (disc is null)
            throw new ArgumentNullException(nameof(disc));

        return disc.Length >= NkitHeader.Offset + 4 && NkitHeader.IsPresent(ReadAt(disc, 0, NkitHeader.Offset + 4));
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
