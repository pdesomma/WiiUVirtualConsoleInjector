using System.Text;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Nintendont's nincfg.bin: the settings it boots a GameCube game with. The autoboot forwarder on an injected title reads the file from the SD card, so it is what decides video, controllers and memory cards for every GameCube inject.
/// </summary>
public sealed class NintendontConfig
{
    /// <summary>
    /// Name of the file at the card's root.
    /// </summary>
    public const string FileName = "nincfg.bin";
    /// <summary>
    /// First word of the file.
    /// </summary>
    public const uint Magic = 0x01070CF6;
    /// <summary>
    /// Layout version this writes; older files are read too.
    /// </summary>
    public const uint Version = 0x0000000A;
    /// <summary>
    /// Bytes the current layout spans.
    /// </summary>
    public const int Size = 548;
    /// <summary>
    /// Most controllers Nintendont takes.
    /// </summary>
    public const int MaxPads = 4;
    /// <summary>
    /// Highest memory card size index.
    /// </summary>
    public const int MaxMemoryCardSize = 5;

    private const int PathSize = 255;
    private const int ConfigOffset = 8;
    private const int VideoOffset = 12;
    private const int LanguageOffset = 16;
    private const int GamePathOffset = 20;
    private const int CheatPathOffset = GamePathOffset + PathSize;
    private const int MaxPadsOffset = 532;
    private const int GameIdOffset = 536;
    private const int MemoryCardOffset = 540;
    private const int VideoScaleOffset = 541;
    private const int VideoShiftOffset = 542;
    private const int NetworkProfileOffset = 543;
    private const int GamePadSlotOffset = 544;

    /// <summary>
    /// Arcade mode: coin-op style controls for the games that support it.
    /// </summary>
    public bool ArcadeMode { get; set; }
    /// <summary>
    /// Boot straight into the game; the forwarder sets this anyway.
    /// </summary>
    public bool AutoBoot { get; set; } = true;
    /// <summary>
    /// Emulate the broadband adapter.
    /// </summary>
    public bool BroadbandEmulation { get; set; }
    /// <summary>
    /// Path of the cheat file when <see cref="Cheats"/> is on and not the default.
    /// </summary>
    public string CheatPath { get; set; } = "";
    /// <summary>
    /// Apply the .gct under sd:/codes for the game.
    /// </summary>
    public bool Cheats { get; set; }
    /// <summary>
    /// Rumble through a Classic Controller Pro.
    /// </summary>
    public bool ClassicControllerRumble { get; set; }
    /// <summary>
    /// Force the game into 16:9.
    /// </summary>
    public bool ForceWidescreen { get; set; }
    /// <summary>
    /// Force 480p.
    /// </summary>
    public bool ForceProgressive { get; set; }
    /// <summary>
    /// Path Nintendont was told to boot; the forwarder replaces it with the disc.
    /// </summary>
    public string GamePath { get; set; } = "";
    /// <summary>
    /// Wii U GamePad slot, 0 to 3.
    /// </summary>
    public uint GamePadSlot { get; set; }
    /// <summary>
    /// Language the game sees.
    /// </summary>
    public NintendontLanguage Language { get; set; } = NintendontLanguage.Auto;
    /// <summary>
    /// Write a log to the card.
    /// </summary>
    public bool Log { get; set; }
    /// <summary>
    /// Controllers to expose, 0 to 4.
    /// </summary>
    public uint Pads { get; set; } = MaxPads;
    /// <summary>
    /// Emulate a memory card on the SD card instead of using a real one.
    /// </summary>
    public bool MemoryCardEmulation { get; set; } = true;
    /// <summary>
    /// One memory card image shared by every game rather than one per game.
    /// </summary>
    public bool MemoryCardShared { get; set; }
    /// <summary>
    /// Size index of the emulated card: 0 is 59 blocks, each step doubles, 5 is 2043.
    /// </summary>
    public int MemoryCardSize { get; set; } = 2;
    /// <summary>
    /// Use a real GameCube controller port (Wii only).
    /// </summary>
    public bool NativeControl { get; set; }
    /// <summary>
    /// Network profile index, 0 to 3.
    /// </summary>
    public byte NetworkProfile { get; set; }
    /// <summary>
    /// Hold the game's PAL50 request but patch it instead of forcing.
    /// </summary>
    public bool PatchPal50 { get; set; }
    /// <summary>
    /// Show progressive as available to the game.
    /// </summary>
    public bool Progressive { get; set; }
    /// <summary>
    /// Lift the 480p limit on the Wii U.
    /// </summary>
    public bool RemoveLimit { get; set; }
    /// <summary>
    /// Skip the GameCube boot animation.
    /// </summary>
    public bool SkipIpl { get; set; }
    /// <summary>
    /// The video mode to force when <see cref="Video"/> is a forcing mode.
    /// </summary>
    public NintendontForcedMode ForcedMode { get; set; } = NintendontForcedMode.Ntsc;
    /// <summary>
    /// Whether and how the video mode is forced.
    /// </summary>
    public NintendontVideo Video { get; set; } = NintendontVideo.Auto;
    /// <summary>
    /// Vertical scale adjustment, -20 to 20.
    /// </summary>
    public sbyte VideoScale { get; set; }
    /// <summary>
    /// Vertical shift, -20 to 20.
    /// </summary>
    public sbyte VideoShift { get; set; }
    /// <summary>
    /// Widen the GamePad's picture too on the Wii U.
    /// </summary>
    public bool WiiUWidescreen { get; set; }

    /// <summary>
    /// Reads a nincfg.bin of this layout or an older one; anything past the file's length keeps its default.
    /// </summary>
    /// <param name="bytes">File contents.</param>
    /// <exception cref="InvalidDataException">Wrong magic or a truncated file.</exception>
    public static NintendontConfig Parse(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length < MemoryCardOffset + 1 || ReadUInt32(bytes, 0) != Magic)
            throw new InvalidDataException("Not a Nintendont configuration.");

        var config = ReadUInt32(bytes, ConfigOffset);
        var video = ReadUInt32(bytes, VideoOffset);
        var result = new NintendontConfig
        {
            Cheats = Has(config, 0),
            MemoryCardEmulation = Has(config, 3),
            ForceWidescreen = Has(config, 5),
            ForceProgressive = Has(config, 6),
            AutoBoot = Has(config, 7),
            RemoveLimit = Has(config, 8),
            Log = Has(config, 12),
            MemoryCardShared = Has(config, 13),
            NativeControl = Has(config, 14),
            WiiUWidescreen = Has(config, 15),
            ArcadeMode = Has(config, 16),
            ClassicControllerRumble = Has(config, 17),
            SkipIpl = Has(config, 18),
            BroadbandEmulation = Has(config, 19),
            Video = (NintendontVideo)(video >> 16 & 0x7),
            ForcedMode = (video & 0x8) != 0 ? NintendontForcedMode.MPal : (video & 0x4) != 0 ? NintendontForcedMode.Ntsc : (video & 0x2) != 0 ? NintendontForcedMode.Pal60 : NintendontForcedMode.Pal50,
            Progressive = (video & 0x10) != 0,
            PatchPal50 = (video & 0x20) != 0,
            Language = (NintendontLanguage)(int)ReadUInt32(bytes, LanguageOffset),
            GamePath = ReadString(bytes, GamePathOffset),
            CheatPath = ReadString(bytes, CheatPathOffset),
            Pads = Math.Min(ReadUInt32(bytes, MaxPadsOffset), MaxPads),
            MemoryCardSize = Math.Min(bytes[MemoryCardOffset], (byte)MaxMemoryCardSize),
        };
        if (bytes.Length > VideoShiftOffset)
        {
            result.VideoScale = (sbyte)bytes[VideoScaleOffset];
            result.VideoShift = (sbyte)bytes[VideoShiftOffset];
        }
        if (bytes.Length > NetworkProfileOffset)
            result.NetworkProfile = (byte)(bytes[NetworkProfileOffset] & 3);
        if (bytes.Length >= GamePadSlotOffset + 4)
            result.GamePadSlot = Math.Min(ReadUInt32(bytes, GamePadSlotOffset), 3);
        return result;
    }

    /// <summary>
    /// The file, in the console's byte order.
    /// </summary>
    public byte[] ToBytes()
    {
        if (Pads > MaxPads)
            throw new InvalidOperationException($"At most {MaxPads} controllers.");
        if (MemoryCardSize < 0 || MemoryCardSize > MaxMemoryCardSize)
            throw new InvalidOperationException($"Memory card size index is 0 to {MaxMemoryCardSize}.");

        var bytes = new byte[Size];
        WriteUInt32(bytes, 0, Magic);
        WriteUInt32(bytes, 4, Version);
        uint config = 0;
        Set(ref config, 0, Cheats);
        Set(ref config, 3, MemoryCardEmulation);
        Set(ref config, 4, Cheats && CheatPath.Length > 0);
        Set(ref config, 5, ForceWidescreen);
        Set(ref config, 6, ForceProgressive);
        Set(ref config, 7, AutoBoot);
        Set(ref config, 8, RemoveLimit);
        Set(ref config, 12, Log);
        Set(ref config, 13, MemoryCardShared);
        Set(ref config, 14, NativeControl);
        Set(ref config, 15, WiiUWidescreen);
        Set(ref config, 16, ArcadeMode);
        Set(ref config, 17, ClassicControllerRumble);
        Set(ref config, 18, SkipIpl);
        Set(ref config, 19, BroadbandEmulation);
        WriteUInt32(bytes, ConfigOffset, config);

        var video = (uint)Video << 16;
        if (Video is NintendontVideo.Force or NintendontVideo.ForceDeflicker)
            video |= 1u << (int)ForcedMode;
        if (Progressive)
            video |= 0x10;
        if (PatchPal50)
            video |= 0x20;
        WriteUInt32(bytes, VideoOffset, video);
        WriteUInt32(bytes, LanguageOffset, unchecked((uint)(int)Language));
        WriteString(bytes, GamePathOffset, GamePath);
        WriteString(bytes, CheatPathOffset, CheatPath);
        WriteUInt32(bytes, MaxPadsOffset, Pads);
        WriteUInt32(bytes, GameIdOffset, 0);
        bytes[MemoryCardOffset] = (byte)MemoryCardSize;
        bytes[VideoScaleOffset] = (byte)VideoScale;
        bytes[VideoShiftOffset] = (byte)VideoShift;
        bytes[NetworkProfileOffset] = (byte)(NetworkProfile & 3);
        WriteUInt32(bytes, GamePadSlotOffset, Math.Min(GamePadSlot, 3));
        return bytes;
    }

    /// <summary>
    /// Blocks an emulated card of a size index holds.
    /// </summary>
    /// <param name="sizeIndex">0 to 5.</param>
    public static int MemoryCardBlocks(int sizeIndex) => (1 << (sizeIndex + 6)) - 5;

    private static bool Has(uint config, int bit) => (config & 1u << bit) != 0;

    private static uint ReadUInt32(byte[] bytes, int offset) => (uint)(bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3]);

    private static string ReadString(byte[] bytes, int offset)
    {
        var end = Array.IndexOf(bytes, (byte)0, offset, PathSize);
        return Encoding.ASCII.GetString(bytes, offset, (end < 0 ? offset + PathSize : end) - offset);
    }

    private static void Set(ref uint config, int bit, bool on)
    {
        if (on)
            config |= 1u << bit;
    }

    private static void WriteString(byte[] bytes, int offset, string value)
    {
        var text = Encoding.ASCII.GetBytes(value ?? "");
        Array.Copy(text, 0, bytes, offset, Math.Min(text.Length, PathSize - 1));
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
