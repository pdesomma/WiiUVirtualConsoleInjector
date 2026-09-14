using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// The shared NES/SNES path: load the executable, replace the ROM, patch the display, save compressed.
/// </summary>
internal static class RetroExecutable
{
    public static string Locate(TitleDirectory title)
    {
        var candidates = Directory.GetFiles(title.Code, "*.rpx");
        if (candidates.Length == 0)
            throw new FileNotFoundException($"No .rpx in {title.Code}.");
        return candidates.OrderBy(p => p, StringComparer.Ordinal).First();
    }

    public static void Inject(Injection injection, TitleDirectory title, bool nes, bool pixelPerfect, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var path = Locate(title);
        progress?.Report("Loading " + Path.GetFileName(path));
        var rpx = RpxFile.Load(path);
        cancellationToken.ThrowIfCancellationRequested();

        var slot = RomSlot.Find(rpx);
        if (slot.IsNes != nes)
            throw new InvalidDataException(nes ? "The base executable is not a NES title." : "The base executable is a NES title, not SNES.");

        progress?.Report($"Injecting {Path.GetFileName(injection.Rom.Path)}");
        slot.Write(File.ReadAllBytes(injection.Rom.Path));

        if (pixelPerfect)
        {
            progress?.Report("Applying pixel-perfect display patch");
            if (!AspectRatioPatch.Apply(rpx, AspectRatio.PixelPerfect, nes))
                throw new InvalidDataException("Display-size instructions not found; cannot apply the pixel-perfect patch.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report("Compressing " + Path.GetFileName(path));
        rpx.Save(path, compress: true);
    }
}
