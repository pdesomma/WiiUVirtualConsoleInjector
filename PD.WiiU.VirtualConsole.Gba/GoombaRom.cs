namespace PD.WiiU.VirtualConsole.Gba;

/// <summary>
/// Wraps a Game Boy or Game Boy Color ROM in the Goomba emulator so the GBA base can run it.
/// </summary>
public static class GoombaRom
{
    /// <summary>
    /// Size the wrapped ROM is padded to.
    /// </summary>
    public const int PaddedSize = 32 * 1024 * 1024;
    /// <summary>
    /// Name of the embedded Goomba build.
    /// </summary>
    public const string ResourceName = "goomba.gba";

    /// <summary>
    /// The Goomba Color build shipped with the injector.
    /// </summary>
    public static byte[] Embedded()
    {
        using var stream = typeof(GoombaRom).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded goomba.gba is missing.");
        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// True for a .gb or .gbc file name.
    /// </summary>
    /// <param name="path">ROM path.</param>
    public static bool IsGameBoy(string path)
    {
        var extension = Path.GetExtension(path ?? throw new ArgumentNullException(nameof(path)));
        return string.Equals(extension, ".gb", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".gbc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".sgb", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Goomba followed by the ROM, zero-padded to <see cref="PaddedSize"/>.
    /// </summary>
    /// <param name="rom">Game Boy ROM.</param>
    /// <param name="goomba">Goomba build; the embedded one when null.</param>
    /// <exception cref="ArgumentException">Goomba plus ROM exceeds the padded size.</exception>
    public static byte[] Wrap(byte[] rom, byte[]? goomba = null)
    {
        if (rom is null)
            throw new ArgumentNullException(nameof(rom));

        goomba ??= Embedded();
        if ((long)goomba.Length + rom.Length > PaddedSize)
            throw new ArgumentException($"Goomba plus ROM is {goomba.Length + rom.Length} bytes; the limit is {PaddedSize}.", nameof(rom));

        var wrapped = new byte[PaddedSize];
        goomba.CopyTo(wrapped, 0);
        rom.CopyTo(wrapped, goomba.Length);
        return wrapped;
    }
}
