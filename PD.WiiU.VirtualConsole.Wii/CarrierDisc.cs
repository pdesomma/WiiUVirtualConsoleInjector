using System.Text;
using WiiSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Builds a homebrew carrier disc around a main.dol and files, using the base title's own disc for the apploader and certificates, and stores it as the title's NFS.
/// </summary>
public static class CarrierDisc
{
    private const string PayloadFileName = "carrier.iso";

    /// <summary>
    /// Replaces the base disc with a carrier and writes its ticket and TMD next to the firmware.
    /// </summary>
    /// <param name="title">Staged base title.</param>
    /// <param name="gameId">Six-character game ID for the carrier.</param>
    /// <param name="discTitle">Disc title; non-ASCII becomes '?'.</param>
    /// <param name="mainDol">Executable to boot.</param>
    /// <param name="files">Files to place at the partition root.</param>
    /// <param name="progress">Step messages.</param>
    /// <param name="cancellationToken">Cancels between hash groups or NFS sectors.</param>
    public static WiiDiscBuildResult Write(TitleDirectory title, string gameId, string discTitle, byte[] mainDol, IReadOnlyList<DiscFile> files, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (files is null)
            throw new ArgumentNullException(nameof(files));

        var key = NfsKey.FromFile(Path.Combine(title.Code, WiiRomInjector.NfsKeyFileName));
        progress?.Report("Reading base disc");
        var (system, region) = ReadBase(title.Content, key);

        var builder = new WiiDiscBuilder(gameId, Ascii(discTitle), system, mainDol) { Region = region };
        foreach (var file in files)
            builder.Files.Add(file);

        var payloadPath = Path.Combine(title.Content, PayloadFileName);
        WiiDiscBuildResult result;
        try
        {
            using var payload = new FileStream(payloadPath, FileMode.Create, FileAccess.ReadWrite);
            progress?.Report("Building carrier disc");
            result = builder.Build(payload, cancellationToken);

            progress?.Report("Writing NFS container");
            payload.Position = 0;
            new NfsWriter(key).Write(payload, new DiscDataSpan(result.Partition.Start, result.Partition.Length), title.Content, cancellationToken: cancellationToken);
        }
        finally
        {
            File.Delete(payloadPath);
        }

        foreach (var stale in Directory.GetFiles(title.Code, "rvlt.*"))
            File.Delete(stale);
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.TicketFileName), result.Ticket.ToBytes());
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.TmdFileName), result.Tmd.ToBytes());
        return result;
    }

    /// <summary>
    /// Title with anything outside printable ASCII replaced and cut to what a disc header holds.
    /// </summary>
    /// <param name="title">Any string.</param>
    public static string Ascii(string title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var builder = new StringBuilder(Math.Min(title.Length, 0x40));
        foreach (var c in title.Take(0x40))
            builder.Append(c < 0x20 || c > 0x7E ? '?' : c);
        return builder.ToString();
    }

    private static (PartitionSystemFiles System, RegionSettings Region) ReadBase(string content, NfsKey key)
    {
        using var payload = NfsReader.Open(content, key).OpenPayload();
        var disc = WiiDisc.Read(payload);
        if (disc.DataPartitions.Count == 0)
            throw new InvalidDataException("Base disc has no data partition.");

        return (PartitionSystemFiles.Read(payload, disc.DataPartitions[0]), RegionArea.Read(payload));
    }
}
