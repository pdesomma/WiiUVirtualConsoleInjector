namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// An injector whose base bounds the ROM: the ROM overwrites the base game's own in place, so it can be no larger.
/// </summary>
public interface IRomCapacity
{
    /// <summary>
    /// Bytes of ROM the base takes, as the ROM file is measured.
    /// </summary>
    /// <param name="title">The base, unpacked.</param>
    /// <exception cref="FileNotFoundException">No executable in the base.</exception>
    /// <exception cref="InvalidDataException">The executable is not a NES or SNES title.</exception>
    long Capacity(TitleDirectory title);

    /// <summary>
    /// Bytes of the ROM that will go into the base: the file, less any header the injector drops.
    /// </summary>
    /// <param name="romPath">The ROM file.</param>
    long RomSize(string romPath);
}
