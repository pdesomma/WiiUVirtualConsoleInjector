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

    public static IReadOnlyList<BaseIssue> Inspect(TitleDirectory title, bool nes)
    {
        var inspection = new BaseInspection(title);
        var path = inspection.RequireAny(TitleDirectory.CodeFolder, "*.rpx");
        if (path is null)
            return inspection.Issues;

        var relative = inspection.Relative(path);
        RomSlot? slot = null;
        if (inspection.Parse(relative, () => slot = RomSlot.Find(RpxFile.Load(path))))
            inspection.Require(slot!.IsNes == nes, relative, nes ? "executable is a SNES title" : "executable is a NES title");
        return inspection.Issues;
    }

    /// <summary>
    /// Bytes the base's ROM slot holds.
    /// </summary>
    /// <param name="title">The base, unpacked.</param>
    public static long Capacity(TitleDirectory title) => RomSlot.Find(RpxFile.Load(Locate(title))).Capacity;

    /// <summary>
    /// Bytes of the ROM the slot receives; a SNES copier header is dropped first.
    /// </summary>
    /// <param name="romPath">The ROM file.</param>
    /// <param name="nes">True for NES.</param>
    public static long RomSize(string romPath, bool nes)
    {
        var length = new FileInfo(romPath).Length;
        // a copier header is 512 bytes on top of a power-of-two-ish image
        return !nes && length % 1024 == 512 ? length - 512 : length;
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
        var rom = File.ReadAllBytes(injection.Rom.Path);
        if (!nes && SnesCopierHeader.IsPresent(rom))
        {
            progress?.Report("Dropping the 512-byte copier header");
            rom = SnesCopierHeader.Strip(rom);
        }
        slot.Write(rom);

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
