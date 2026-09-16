using System.Text;
using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;
using WiiSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Injects a retail Wii disc image into a vWii base.
/// </summary>
public sealed class WiiRomInjector : IRomInjector
{
    /// <summary>
    /// Firmware image the patches apply to.
    /// </summary>
    public const string FirmwareFileName = "fw.img";
    /// <summary>
    /// AES key for the NFS container.
    /// </summary>
    public const string NfsKeyFileName = "htk.bin";
    /// <summary>
    /// Disc ticket copied next to the firmware.
    /// </summary>
    public const string TicketFileName = "rvlt.tik";
    /// <summary>
    /// Disc TMD copied next to the firmware.
    /// </summary>
    public const string TmdFileName = "rvlt.tmd";

    /// <summary>
    /// Carrier ID given to a homebrew DOL.
    /// </summary>
    public const string HomebrewGameId = "HBRW01";

    private const string PayloadFileName = "game.iso";
    private const string RebuiltFileName = "rebuilt.iso";

    private readonly WiiPartitionCipher? _cipher;

    /// <summary>
    /// Creates a new instance of the <see cref="WiiRomInjector"/> class.
    /// </summary>
    /// <param name="commonKey">Key that unlocks disc partitions, or null to serve only what needs no decrypt: NKit images, homebrew and channels.</param>
    public WiiRomInjector(CommonKey? commonKey)
    {
        _cipher = commonKey is { } key ? new WiiPartitionCipher(key) : null;
    }

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Wii;

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        return VWiiBase.Inspect(title);
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Not an ISO or WBFS, or more cheat lines than fit.</exception>
    /// <exception cref="InvalidOperationException">An encrypted disc with no common key supplied.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Wii)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as WiiOptions ?? new WiiOptions();

        var extension = Path.GetExtension(injection.Rom.Path);
        if (string.Equals(extension, ".dol", StringComparison.OrdinalIgnoreCase))
        {
            InjectHomebrew(injection.Rom.Path, options, title, progress, cancellationToken);
            return Task.CompletedTask;
        }
        if (string.Equals(extension, ".wad", StringComparison.OrdinalIgnoreCase))
        {
            InjectChannel(injection.Rom.Path, options, title, progress, cancellationToken);
            return Task.CompletedTask;
        }

        using var container = OpenImage(injection.Rom.Path, out var iso);
        using (iso)
        {
            Inject(iso, options, title, progress, cancellationToken);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Six-character carrier ID for a channel: its four-character code plus the retail maker code.
    /// </summary>
    /// <param name="titleId">Eight-byte channel title ID.</param>
    public static string ChannelGameId(byte[] titleId)
    {
        if (titleId is null)
            throw new ArgumentNullException(nameof(titleId));
        if (titleId.Length != 8)
            throw new ArgumentException("A title ID is 8 bytes.", nameof(titleId));

        return Encoding.ASCII.GetString(titleId, 4, 4) + "01";
    }

    /// <summary>
    /// True when injecting <paramref name="path"/> would decrypt a disc: an encrypted .iso or a .wbfs. NKit images, homebrew and channels need no key.
    /// </summary>
    /// <param name="path">ROM path; may not exist yet.</param>
    public static bool NeedsCommonKey(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".wbfs", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.Equals(extension, ".iso", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!File.Exists(path))
            return true;

        using var image = File.OpenRead(path);
        return !WiiDiscRebuilder.IsNkit(image);
    }

    /// <summary>
    /// Opens a .iso, NKit or not, directly or the first disc of a .wbfs; the returned disposable owns the container.
    /// </summary>
    /// <param name="path">Image path.</param>
    /// <param name="image">Seekable disc image.</param>
    /// <exception cref="NotSupportedException">Any other extension.</exception>
    public static IDisposable OpenImage(string path, out Stream image)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".iso", StringComparison.OrdinalIgnoreCase))
        {
            image = File.OpenRead(path);
            return image;
        }
        if (string.Equals(extension, ".wbfs", StringComparison.OrdinalIgnoreCase))
        {
            var wbfs = WbfsFile.Open(path);
            if (wbfs.Discs.Count == 0)
            {
                wbfs.Dispose();
                throw new InvalidDataException("WBFS file holds no disc.");
            }
            image = wbfs.Discs[0].OpenStream();
            return wbfs;
        }
        throw new NotSupportedException("Only .iso and .wbfs images are supported.");
    }

    private static void InjectChannel(string wadPath, WiiOptions options, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var wad = WadFile.Open(wadPath);
        var gameId = ChannelGameId(wad.TitleId);
        var booter = options.ForwarderPath is null ? ChannelBooter.Embedded(options.ForceFourByThree) : File.ReadAllBytes(options.ForwarderPath);
        progress?.Report($"Forwarding to channel {gameId.Substring(0, 4)}");

        using var code = new MemoryStream(wad.TitleId.Skip(4).Take(4).ToArray());
        CarrierDisc.Write(title, gameId, "Channel " + gameId.Substring(0, 4), booter, new[] { new DiscFile(ChannelBooter.TitleFileName, code) }, progress, cancellationToken);
        PatchFirmware(title, options, progress);
    }

    private static void InjectHomebrew(string dolPath, WiiOptions options, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var dol = File.ReadAllBytes(dolPath);
        DolHeader.Parse(dol);
        CarrierDisc.Write(title, HomebrewGameId, Path.GetFileNameWithoutExtension(dolPath), dol, Array.Empty<DiscFile>(), progress, cancellationToken);
        PatchFirmware(title, options, progress);
    }

    private static void PatchFirmware(TitleDirectory title, WiiOptions options, IProgress<string>? progress)
    {
        progress?.Report("Patching " + FirmwareFileName);
        FirmwarePatcher.PatchFile(Path.Combine(title.Code, FirmwareFileName), FirmwarePatcher.PatchesFor(options, homebrew: true));
    }

    private void Inject(Stream iso, WiiOptions options, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var disc = WiiDisc.Read(iso);
        if (disc.DataPartitions.Count == 0)
            throw new InvalidDataException("Disc has no data partition.");

        if (!WiiDiscRebuilder.IsNkit(iso) && _cipher is null)
            throw new InvalidOperationException("The Wii common key is needed to read an encrypted disc image.");

        var (ticket, tmd) = WiiDiscRebuilder.IsNkit(iso)
            ? WriteNfsFromNkit(iso, options, title, progress, cancellationToken)
            : WriteNfs(iso, disc.DataPartitions[0], options, title, progress, cancellationToken);
        WriteTicketAndTmd(title, ticket, tmd);

        progress?.Report("Patching " + FirmwareFileName);
        FirmwarePatcher.PatchFile(Path.Combine(title.Code, FirmwareFileName), FirmwarePatcher.PatchesFor(options));

        VWiiMeta.Apply(title, disc.Header.GameId);
    }

    /// <summary>
    /// Applies the requested main.dol patches; reports what was found.
    /// </summary>
    /// <param name="dol">main.dol.</param>
    /// <param name="options">Wii settings.</param>
    /// <param name="progress">Receives one line per patch.</param>
    /// <returns>The patched copy.</returns>
    public static byte[] PatchMainDol(byte[] dol, WiiOptions options, IProgress<string>? progress = null)
    {
        if (dol is null)
            throw new ArgumentNullException(nameof(dol));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var patched = (byte[])dol.Clone();
        if (options.RemoveDeflicker)
        {
            var found = DolFilterPatches.RemoveDeflicker(patched);
            progress?.Report(found ? "Deflicker filter removed" : "Deflicker pattern not found");
        }
        if (options.RemoveDithering)
        {
            var found = DolFilterPatches.RemoveDithering(patched);
            progress?.Report(found ? "Dithering removed" : "Dithering pattern not found");
        }
        if (options.HalfVerticalFilter)
        {
            var count = DolFilterPatches.HalveVerticalFilter(patched);
            progress?.Report($"Vertical filters halved: {count}");
        }
        if (options.VideoMode != WiiVideoMode.Unchanged)
        {
            var count = VideoModePatch.Apply(patched, options.VideoMode);
            progress?.Report($"Video modes set to {options.VideoMode}: {count}");
        }
        if (options.CheatCodesPath is not null)
        {
            var lines = GeckoCodes.Load(options.CheatCodesPath);
            patched = GeckoCheatPatch.Apply(patched, lines);
            progress?.Report($"Gecko codes baked in: {lines.Count} lines");
        }
        return patched;
    }

    /// <summary>
    /// True when any option needs main.dol rewritten.
    /// </summary>
    /// <param name="options">Wii settings.</param>
    public static bool PatchesMainDol(WiiOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        return options.RemoveDeflicker || options.RemoveDithering || options.HalfVerticalFilter || options.VideoMode != WiiVideoMode.Unchanged || options.CheatCodesPath is not null;
    }

    private static byte[] ReadTmd(Stream iso, Partition partition)
    {
        var tmd = new byte[partition.Header.TmdSize];
        iso.Position = partition.Offset + partition.Header.TmdOffset;
        var read = 0;
        while (read < tmd.Length)
        {
            var n = iso.Read(tmd, read, tmd.Length - read);
            if (n == 0)
                throw new EndOfStreamException("Disc image ends inside the TMD.");
            read += n;
        }
        return tmd;
    }

    private static void WriteTicketAndTmd(TitleDirectory title, byte[] ticket, byte[] tmd)
    {
        foreach (var stale in Directory.GetFiles(title.Code, "rvlt.*"))
            File.Delete(stale);

        File.WriteAllBytes(Path.Combine(title.Code, TicketFileName), ticket);
        File.WriteAllBytes(Path.Combine(title.Code, TmdFileName), tmd);
    }

    /// <summary>
    /// An NKit image is already plaintext, so it goes straight through the rebuilder, which lays its files out fresh with hashes.
    /// </summary>
    private static (byte[] Ticket, byte[] Tmd) WriteNfsFromNkit(Stream nkit, WiiOptions options, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var key = NfsKey.FromFile(Path.Combine(title.Code, NfsKeyFileName));
        var rebuiltPath = Path.Combine(title.Content, RebuiltFileName);
        try
        {
            progress?.Report("Expanding NKit image");
            using var rebuilt = new FileStream(rebuiltPath, FileMode.Create, FileAccess.ReadWrite);
            var result = WiiDiscRebuilder.Rebuild(nkit, rebuilt, PatchesMainDol(options) ? dol => PatchMainDol(dol, options, progress) : null, cancellationToken);
            if (RegionPatcher.Apply(rebuilt, options))
                progress?.Report($"Region set to {options.TargetRegion}");

            progress?.Report("Writing NFS container");
            rebuilt.Position = 0;
            new NfsWriter(key).Write(rebuilt, new DiscDataSpan(result.Partition.Start, result.Partition.Length), title.Content, cancellationToken: cancellationToken);
            return (result.Ticket.ToBytes(), result.Tmd.ToBytes());
        }
        finally
        {
            File.Delete(rebuiltPath);
        }
    }

    private (byte[] Ticket, byte[] Tmd) WriteNfs(Stream iso, Partition partition, WiiOptions options, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var key = NfsKey.FromFile(Path.Combine(title.Code, NfsKeyFileName));
        var payloadPath = Path.Combine(title.Content, PayloadFileName);
        var rebuiltPath = Path.Combine(title.Content, RebuiltFileName);
        try
        {
            var payload = new FileStream(payloadPath, FileMode.Create, FileAccess.ReadWrite);
            using (payload)
            {
                progress?.Report("Decrypting disc");
                var span = _cipher!.Decrypt(iso, payload, cancellationToken);

                if (RegionPatcher.Apply(payload, options))
                    progress?.Report($"Region set to {options.TargetRegion}");

                if (!options.TrimDisc && !PatchesMainDol(options))
                {
                    progress?.Report("Writing NFS container");
                    new NfsWriter(key).Write(payload, span, title.Content, cancellationToken: cancellationToken);
                    return (partition.Ticket.ToBytes(), ReadTmd(iso, partition));
                }

                progress?.Report("Rebuilding disc");
                using var rebuilt = new FileStream(rebuiltPath, FileMode.Create, FileAccess.ReadWrite);
                var result = WiiDiscRebuilder.Rebuild(payload, rebuilt, PatchesMainDol(options) ? dol => PatchMainDol(dol, options, progress) : null, cancellationToken);
                payload.Dispose();
                File.Delete(payloadPath);

                progress?.Report("Writing NFS container");
                rebuilt.Position = 0;
                new NfsWriter(key).Write(rebuilt, new DiscDataSpan(result.Partition.Start, result.Partition.Length), title.Content, cancellationToken: cancellationToken);
                return (result.Ticket.ToBytes(), result.Tmd.ToBytes());
            }
        }
        finally
        {
            File.Delete(payloadPath);
            File.Delete(rebuiltPath);
        }
    }
}
