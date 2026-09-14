using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;
using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Wraps a GameCube image in a carrier Wii disc that boots the Nintendont forwarder.
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

    private static readonly FirmwarePatch[] Patches = { FirmwarePatch.FakeSign, FirmwarePatch.Homebrew, FirmwarePatch.Passthrough };

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.GameCube;

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        return VWiiBase.Inspect(title);
    }

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
        var forwarder = options.ForwarderPath is null ? NintendontForwarder.Embedded(options.ForceFourByThree) : File.ReadAllBytes(options.ForwarderPath);

        using var game = GameCubeImage.Open(injection.Rom.Path, out var gameImage);
        using (gameImage)
        {
            Stream? secondImage = null;
            using var second = options.SecondDiscPath is null ? null : GameCubeImage.Open(options.SecondDiscPath, out secondImage);
            using (secondImage)
            {
                var header = GameCubeImage.ReadHeader(gameImage);
                var files = new List<DiscFile> { new(GameFileName, gameImage) };
                if (secondImage is not null)
                {
                    GameCubeImage.ReadHeader(secondImage);
                    files.Add(new DiscFile(SecondDiscFileName, secondImage));
                }

                CarrierDisc.Write(title, header.GameId, header.Title, forwarder, files, progress, cancellationToken);

                progress?.Report("Patching " + WiiRomInjector.FirmwareFileName);
                FirmwarePatcher.PatchFile(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName), Patches);

                VWiiMeta.Apply(title, header.GameId, gamePadAsController: true);
            }
        }
        return Task.CompletedTask;
    }
}
