using System.Text;
using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;
using WiiSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Wraps a GameCube image in a carrier Wii disc that boots the Nintendont forwarder, reusing the base disc's apploader and certificates.
/// </summary>
public sealed class GameCubeRomInjector : IRomInjector
{
    /// <summary>
    /// Name Nintendont looks for on the disc.
    /// </summary>
    public const string GameFileName = "game.iso";
    /// <summary>
    /// Name of the second disc on the carrier.
    /// </summary>
    public const string SecondDiscFileName = "disc2.iso";

    private const string PayloadFileName = "carrier.iso";

    private static readonly FirmwarePatch[] Patches = { FirmwarePatch.FakeSign, FirmwarePatch.Homebrew, FirmwarePatch.Passthrough };

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.GameCube;

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Not an ISO, GCM or GCZ image.</exception>
    /// <exception cref="InvalidDataException">Not a GameCube disc, or the base has no usable disc.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.GameCube)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as GameCubeOptions ?? new GameCubeOptions();
        var key = NfsKey.FromFile(Path.Combine(title.Code, WiiRomInjector.NfsKeyFileName));

        progress?.Report("Reading base disc");
        var (system, region) = ReadBase(title.Content, key);
        var forwarder = options.ForwarderPath is null ? NintendontForwarder.Embedded(options.ForceFourByThree) : File.ReadAllBytes(options.ForwarderPath);

        using var game = GameCubeImage.Open(injection.Rom.Path, out var gameImage);
        using (gameImage)
        {
            Stream? secondImage = null;
            using var second = options.SecondDiscPath is null ? null : GameCubeImage.Open(options.SecondDiscPath, out secondImage);
            using (secondImage)
            {
                var header = GameCubeImage.ReadHeader(gameImage);
                var builder = new WiiDiscBuilder(header.GameId, Ascii(header.Title), system, forwarder) { Region = region };
                builder.Files.Add(new DiscFile(GameFileName, gameImage));
                if (secondImage is not null)
                {
                    GameCubeImage.ReadHeader(secondImage);
                    builder.Files.Add(new DiscFile(SecondDiscFileName, secondImage));
                }

                var result = WriteNfs(builder, key, title, progress, cancellationToken);
                WriteTicketAndTmd(result, title);

                progress?.Report("Patching " + WiiRomInjector.FirmwareFileName);
                FirmwarePatcher.PatchFile(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName), Patches);

                VWiiMeta.Apply(title, header.GameId, gamePadAsController: true);
            }
        }
        return Task.CompletedTask;
    }

    private static string Ascii(string title)
    {
        var builder = new StringBuilder(title.Length);
        foreach (var c in title)
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

    private static WiiDiscBuildResult WriteNfs(WiiDiscBuilder builder, NfsKey key, TitleDirectory title, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var payloadPath = Path.Combine(title.Content, PayloadFileName);
        try
        {
            using var payload = new FileStream(payloadPath, FileMode.Create, FileAccess.ReadWrite);
            progress?.Report("Building carrier disc");
            var result = builder.Build(payload, cancellationToken);

            progress?.Report("Writing NFS container");
            payload.Position = 0;
            new NfsWriter(key).Write(payload, new DiscDataSpan(result.Partition.Start, result.Partition.Length), title.Content, cancellationToken: cancellationToken);
            return result;
        }
        finally
        {
            File.Delete(payloadPath);
        }
    }

    private static void WriteTicketAndTmd(WiiDiscBuildResult result, TitleDirectory title)
    {
        foreach (var stale in Directory.GetFiles(title.Code, "rvlt.*"))
            File.Delete(stale);

        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.TicketFileName), result.Ticket.ToBytes());
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.TmdFileName), result.Tmd.ToBytes());
    }
}
