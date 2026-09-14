using System.Text;
using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;
using WiiSharp;
using WiiUSharp;
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

    private const string PayloadFileName = "game.iso";

    private readonly WiiPartitionCipher _cipher;

    /// <summary>
    /// Creates a new instance of the <see cref="WiiRomInjector"/> class.
    /// </summary>
    /// <param name="commonKey">Key that unlocks disc partitions.</param>
    public WiiRomInjector(CommonKey commonKey)
    {
        _cipher = new WiiPartitionCipher(commonKey);
    }

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Wii;

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Not an ISO or WBFS, or an option this injector cannot apply yet.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Wii)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as WiiOptions ?? new WiiOptions();
        RejectUnsupported(options);

        using var container = OpenImage(injection.Rom.Path, out var iso);
        using (iso)
        {
            Inject(iso, options, title, progress, cancellationToken);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens a .iso directly or the first disc of a .wbfs; the returned disposable owns the container.
    /// </summary>
    /// <param name="path">Image path.</param>
    /// <param name="image">Seekable disc image.</param>
    /// <exception cref="NotSupportedException">Any other extension, or an NKit image.</exception>
    public static IDisposable OpenImage(string path, out Stream image)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        if (path.IndexOf(".nkit.", StringComparison.OrdinalIgnoreCase) >= 0)
            throw new NotSupportedException("NKit images are not supported; convert to a plain ISO or WBFS first.");

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

    private void Inject(Stream iso, WiiOptions options, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var disc = WiiDisc.Read(iso);
        if (disc.DataPartitions.Count == 0)
            throw new InvalidDataException("Disc has no data partition.");

        WriteNfs(iso, options, title, progress, cancellationToken);
        CopyTicketAndTmd(iso, disc.DataPartitions[0], title);

        progress?.Report("Patching " + FirmwareFileName);
        FirmwarePatcher.PatchFile(Path.Combine(title.Code, FirmwareFileName), FirmwarePatcher.PatchesFor(options));

        SetManualId(title, disc.Header.GameId);
    }

    private static void CopyTicketAndTmd(Stream iso, Partition partition, TitleDirectory title)
    {
        foreach (var stale in Directory.GetFiles(title.Code, "rvlt.*"))
            File.Delete(stale);

        File.WriteAllBytes(Path.Combine(title.Code, TicketFileName), partition.Ticket.ToBytes());

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
        File.WriteAllBytes(Path.Combine(title.Code, TmdFileName), tmd);
    }

    private static void RejectUnsupported(WiiOptions options)
    {
        if (!options.TrimDisc)
            throw new NotSupportedException("Untrimmed discs are not supported yet.");
        if (options.CheatCodesPath is not null)
            throw new NotSupportedException("Cheat codes are not supported yet.");
        if (options.ForcePal || options.HalfVerticalFilter || options.RemoveDeflicker || options.RemoveDithering)
            throw new NotSupportedException("main.dol patches are not supported yet.");
    }

    private static void SetManualId(TitleDirectory title, string gameId)
    {
        var hex = new StringBuilder();
        foreach (var b in Encoding.ASCII.GetBytes(gameId.Substring(0, 4)))
            hex.Append(b.ToString("x2"));

        var meta = MetaXml.Load(title.MetaXmlPath);
        meta.Set("reserved_flag2", hex.ToString());
        meta.Save(title.MetaXmlPath);
    }

    private void WriteNfs(Stream iso, WiiOptions options, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var key = NfsKey.FromFile(Path.Combine(title.Code, NfsKeyFileName));
        var payloadPath = Path.Combine(title.Content, PayloadFileName);
        try
        {
            using var payload = new FileStream(payloadPath, FileMode.Create, FileAccess.ReadWrite);
            progress?.Report("Decrypting disc");
            var span = _cipher.Decrypt(iso, payload, cancellationToken);

            if (RegionPatcher.Apply(payload, options))
                progress?.Report($"Region set to {options.TargetRegion}");

            progress?.Report("Writing NFS container");
            new NfsWriter(key).Write(payload, span, title.Content, cancellationToken: cancellationToken);
        }
        finally
        {
            File.Delete(payloadPath);
        }
    }
}
