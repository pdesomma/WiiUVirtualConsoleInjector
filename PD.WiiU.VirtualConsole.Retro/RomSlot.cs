using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Where the NES or SNES ROM lives inside a Virtual Console executable, found by the WUP- marker.
/// </summary>
public sealed class RomSlot
{
    private static readonly byte[] Marker = { 0x57, 0x55, 0x50, 0x2D };
    private static readonly byte[] NesMagic = { 0x4E, 0x45, 0x53, 0x1A };
    private static readonly byte[] NesMagicAlt = { 0x4E, 0x45, 0x53, 0x00 };

    private RomSlot(Section section, int offset, int capacity, bool isNes)
    {
        Section = section;
        Offset = offset;
        Capacity = capacity;
        IsNes = isNes;
    }

    /// <summary>
    /// Bytes the slot can hold; the NES figure includes the sixteen-byte iNES header.
    /// </summary>
    public int Capacity { get; }
    /// <summary>
    /// True for a NES executable, false for SNES.
    /// </summary>
    public bool IsNes { get; }
    /// <summary>
    /// Offset of the ROM within the section data.
    /// </summary>
    public int Offset { get; }
    /// <summary>
    /// Section holding the ROM.
    /// </summary>
    public Section Section { get; }

    /// <summary>
    /// Finds the slot in a loaded, plain executable.
    /// </summary>
    /// <param name="rpx">Executable with sections plain.</param>
    /// <exception cref="InvalidDataException">No WUP- marker, or an unknown size key.</exception>
    public static RomSlot Find(RpxFile rpx)
    {
        if (rpx is null)
            throw new ArgumentNullException(nameof(rpx));

        foreach (var section in rpx.Sections.Where(s => s.HasData).OrderBy(s => s.StoredOffset))
        {
            var data = section.Data;
            for (var i = 0; i + Marker.Length <= data.Length; i += 4)
            {
                if (!Matches(data, i, Marker) || i + 20 > data.Length)
                    continue;

                var nes = Matches(data, i + 16, NesMagic) || Matches(data, i + 16, NesMagicAlt);
                var keyAt = i - (nes ? 14 : 22);
                if (keyAt < 0)
                    throw new InvalidDataException("WUP- marker sits too close to the start of its section.");
                var capacity = CapacityFor(data[keyAt]) + (nes ? 16 : 0);
                return new RomSlot(section, i + (nes ? 16 : 12), capacity, nes);
            }
        }
        throw new InvalidDataException("No WUP- marker found; not a NES or SNES Virtual Console executable.");
    }

    /// <summary>
    /// True when the file starts with the iNES magic.
    /// </summary>
    /// <param name="rom">ROM bytes.</param>
    public static bool IsNesRom(byte[] rom) => rom is not null && rom.Length >= 4 && Matches(rom, 0, NesMagic);

    /// <summary>
    /// Overwrites the slot with the ROM; bytes past the ROM keep the base game.
    /// </summary>
    /// <param name="rom">ROM bytes.</param>
    /// <exception cref="ArgumentException">ROM larger than the slot, or NES/SNES mismatch.</exception>
    public void Write(byte[] rom)
    {
        if (rom is null)
            throw new ArgumentNullException(nameof(rom));
        if (rom.Length > Capacity)
            throw new ArgumentException($"ROM is {rom.Length} bytes; the base holds at most {Capacity}.", nameof(rom));
        if (IsNesRom(rom) != IsNes)
            throw new ArgumentException(IsNes ? "The base is NES but the ROM is not." : "The base is SNES but the ROM is a NES file.", nameof(rom));
        if (Offset + rom.Length > Section.Data.Length)
            throw new InvalidDataException("The slot runs past the end of its section.");

        var data = (byte[])Section.Data.Clone();
        rom.CopyTo(data, Offset);
        Section.Data = data;
    }

    private static int CapacityFor(byte key) => key switch
    {
        0x00 => 0x8000,
        0x01 => 0x10000,
        0x02 => 0x20000,
        0x0C => 0x20000,
        0x04 => 0x40000,
        0x06 => 0x60000,
        0x08 => 0x80000,
        0x10 => 0x100000,
        0x20 => 0x200000,
        0x30 => 0x300000,
        0x40 => 0x400000,
        _ => throw new InvalidDataException($"Unknown ROM size key 0x{key:X2}."),
    };

    private static bool Matches(byte[] data, int at, byte[] pattern)
    {
        if (at < 0 || at + pattern.Length > data.Length)
            return false;
        for (var i = 0; i < pattern.Length; i++)
            if (data[at + i] != pattern[i])
                return false;
        return true;
    }
}
