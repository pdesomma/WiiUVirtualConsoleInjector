using PD.WiiU.VirtualConsole.Infrastructure;
using PD.WiiU.VirtualConsole.Options;
using TargaSharp;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class JsonInjectionHistoryTests
{
    private string _root = null!;
    private string _folder = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        _folder = Path.Combine(_root, "history");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Constructor_BlankFolder_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new JsonInjectionHistory(" "));
    }

    [TestMethod]
    public void All_NoFolder_IsEmptyAndCreatesNothing()
    {
        var history = new JsonInjectionHistory(_folder);

        Assert.AreEqual(0, history.All().Count);
        Assert.IsFalse(Directory.Exists(_folder));
    }

    [TestMethod]
    public void Add_Record_CopiesFilesWritesIconAndRoundTripsThroughAFreshInstance()
    {
        var sources = Path.Combine(_root, "src");
        Directory.CreateDirectory(sources);
        var icon = Write(sources, "shot.png", 1);
        var tv = Write(sources, "tv.jpg", 2);
        var sound = Write(sources, "boot.wav", 3);
        var bare = Record("one", "Super Game, The Sequel");
        var record = new InjectionRecord(bare.Id, bare.CreatedAt, bare.Console, bare.BaseTitleId, bare.RomPath, bare.Name, bare.Identity)
        {
            Artwork = new Artwork { Icon = icon, BootTv = tv, BootDrc = @"C:\gone\drc.png" },
            BootSoundPath = sound,
            Format = OutputFormat.Loadiine,
            GamePad = true,
            Options = new WiiOptions { VideoMode = WiiVideoMode.Pal60, LrPatch = true, TargetRegion = Region.Europe, Passthrough = false },
            OutputDirectory = @"D:\out\[WUP]Super Game",
            ProductId = "WXYZ",
            ShortName = "Super",
        };
        var changes = 0;
        var history = new JsonInjectionHistory(_folder);
        history.Changed += (_, _) => changes++;

        var stored = history.Add(record, IconTga());

        var home = Path.Combine(_folder, "one");
        Assert.AreEqual(1, changes);
        Assert.AreEqual(Path.Combine(home, "iconTex.png"), stored.Artwork.Icon);
        Assert.AreEqual(Path.Combine(home, "bootTvTex.jpg"), stored.Artwork.BootTv);
        Assert.AreEqual(@"C:\gone\drc.png", stored.Artwork.BootDrc, "a source that is already gone keeps its path");
        Assert.IsNull(stored.Artwork.BootLogo);
        Assert.AreEqual(Path.Combine(home, "bootSound.wav"), stored.BootSoundPath);
        Assert.AreEqual(Path.Combine(home, "icon.png"), stored.IconPath);
        CollectionAssert.AreEqual(new byte[] { 1 }, File.ReadAllBytes(stored.Artwork.Icon!));
        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, File.ReadAllBytes(stored.IconPath!).Take(4).ToArray(), "PNG magic");

        var reloaded = new JsonInjectionHistory(_folder).All().Single();
        Assert.AreEqual("one", reloaded.Id);
        Assert.AreEqual(record.CreatedAt, reloaded.CreatedAt);
        Assert.AreEqual(SourceConsole.Wii, reloaded.Console);
        Assert.AreEqual(record.BaseTitleId, reloaded.BaseTitleId);
        Assert.AreEqual(record.RomPath, reloaded.RomPath);
        Assert.AreEqual("Super Game, The Sequel", reloaded.Name);
        Assert.AreEqual("Super", reloaded.ShortName);
        Assert.AreEqual("WXYZ", reloaded.ProductId);
        Assert.IsTrue(reloaded.GamePad);
        Assert.AreEqual(OutputFormat.Loadiine, reloaded.Format);
        Assert.AreEqual(record.Identity, reloaded.Identity);
        Assert.AreEqual(stored.Artwork.Icon, reloaded.Artwork.Icon);
        Assert.AreEqual(stored.BootSoundPath, reloaded.BootSoundPath);
        Assert.AreEqual(stored.IconPath, reloaded.IconPath);
        Assert.AreEqual(@"D:\out\[WUP]Super Game", reloaded.OutputDirectory);
        var wii = (WiiOptions)reloaded.Options!;
        Assert.AreEqual(WiiVideoMode.Pal60, wii.VideoMode);
        Assert.IsTrue(wii.LrPatch);
        Assert.IsFalse(wii.Passthrough);
        Assert.AreEqual(Region.Europe, wii.TargetRegion);
    }

    [TestMethod]
    public void Add_TwoRecords_NewestFirst()
    {
        var history = new JsonInjectionHistory(_folder);

        history.Add(Record("first", "First"), null);
        history.Add(Record("second", "Second"), null);

        CollectionAssert.AreEqual(new[] { "second", "first" }, history.All().Select(r => r.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "second", "first" }, new JsonInjectionHistory(_folder).All().Select(r => r.Id).ToArray());
    }

    [TestMethod]
    public void Add_SameIdAgain_ReplacesIt()
    {
        var history = new JsonInjectionHistory(_folder);
        history.Add(Record("one", "Old"), null);

        history.Add(Record("one", "New"), null);

        Assert.AreEqual("New", history.All().Single().Name);
    }

    [TestMethod]
    public void Add_NoIconAndNoFiles_LeavesThoseNull()
    {
        var history = new JsonInjectionHistory(_folder);

        var stored = history.Add(Record("one", "Bare"), null);

        Assert.IsNull(stored.IconPath);
        Assert.IsNull(stored.Artwork.Icon);
        Assert.IsNull(stored.BootSoundPath);
        Assert.IsNull(stored.Options);
    }

    [TestMethod]
    public void Add_UndecodableIcon_SkipsTheIcon()
    {
        var history = new JsonInjectionHistory(_folder);

        var stored = history.Add(Record("one", "Bare"), new byte[] { 1, 2, 3 });

        Assert.IsNull(stored.IconPath);
    }

    [TestMethod]
    public void Add_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new JsonInjectionHistory(_folder).Add(null!, null));
    }

    [TestMethod]
    public void Remove_KnownId_DeletesTheRecordAndItsFolder()
    {
        var history = new JsonInjectionHistory(_folder);
        history.Add(Record("one", "One"), IconTga());
        history.Add(Record("two", "Two"), null);
        var changes = 0;
        history.Changed += (_, _) => changes++;

        history.Remove("one");

        Assert.AreEqual(1, changes);
        Assert.AreEqual("two", history.All().Single().Id);
        Assert.IsFalse(Directory.Exists(Path.Combine(_folder, "one")));
        Assert.AreEqual("two", new JsonInjectionHistory(_folder).All().Single().Id);
    }

    [TestMethod]
    public void Remove_UnknownId_DoesNothing()
    {
        var history = new JsonInjectionHistory(_folder);
        history.Add(Record("one", "One"), null);
        var changes = 0;
        history.Changed += (_, _) => changes++;

        history.Remove("nope");

        Assert.AreEqual(0, changes);
        Assert.AreEqual(1, history.All().Count);
        Assert.ThrowsExactly<ArgumentException>(() => history.Remove(" "));
    }

    [TestMethod]
    public void All_CorruptIndex_ReadsAsEmpty()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, JsonInjectionHistory.IndexFileName), "{ not json");

        Assert.AreEqual(0, new JsonInjectionHistory(_folder).All().Count);
    }

    [TestMethod]
    public void All_RecordWithBadIds_IsSkipped()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, JsonInjectionHistory.IndexFileName),
            "[{\"id\":\"x\",\"romPath\":\"C:\\\\r.nes\",\"name\":\"X\",\"console\":\"Nes\",\"baseTitleId\":\"nope\",\"titleId\":\"0005000231323334\",\"groupId\":\"00003456\",\"productCode\":\"WUP-N-ABCD\"}]");

        Assert.AreEqual(0, new JsonInjectionHistory(_folder).All().Count);
    }

    [TestMethod]
    public void All_OptionsOfTheWrongShape_ReadAsDefaults()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, JsonInjectionHistory.IndexFileName),
            "[{\"id\":\"x\",\"romPath\":\"C:\\\\r.nes\",\"name\":\"X\",\"console\":\"Nes\",\"baseTitleId\":\"0005000010101D00\",\"titleId\":\"0005000231323334\",\"groupId\":\"00003456\",\"productCode\":\"WUP-N-ABCD\",\"options\":\"junk\"}]");

        var record = new JsonInjectionHistory(_folder).All().Single();

        Assert.IsNull(record.Options);
        Assert.AreEqual(SourceConsole.Nes, record.Console);
    }

    [TestMethod]
    public void TgaPng_Encode_RoundTripsA32BitIconAndRejectsJunk()
    {
        var png = TgaPng.Encode(IconTga());

        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4).ToArray());
        using var decoded = SkiaSharp.SKBitmap.Decode(png);
        Assert.AreEqual(2, decoded.Width);
        Assert.AreEqual(2, decoded.Height);
        Assert.AreEqual(new SkiaSharp.SKColor(0, 0, 255, 255), decoded.GetPixel(0, 0), "TGA rows are bottom-up; the top-left pixel is the last row's first");
        Assert.AreEqual(new SkiaSharp.SKColor(255, 0, 0, 255), decoded.GetPixel(0, 1));
        Assert.ThrowsExactly<InvalidDataException>(() => TgaPng.Encode(new byte[] { 1, 2, 3 }));
        Assert.ThrowsExactly<ArgumentNullException>(() => TgaPng.Encode(null!));
    }

    [TestMethod]
    public void TgaPng_Encode_RleTga_ThrowsNotSupportedException()
    {
        var rle = new TgaFile(2, 2, TgaPixelDepth.Bpp24, TgaImageType.RleTrueColor, attrBits: 0, newFormat: false);
        rle.ImageArea.ImageData = new byte[12];

        Assert.ThrowsExactly<NotSupportedException>(() => TgaPng.Encode(rle.ToBytes()));
    }

    /// <summary>
    /// A 2×2 32-bit icon: bottom row red, top row blue, bottom-left origin as titles use.
    /// </summary>
    private static byte[] IconTga()
    {
        var tga = new TgaFile(2, 2, TgaPixelDepth.Bpp32, TgaImageType.UncompressedTrueColor, attrBits: 8, newFormat: false);
        tga.Header.ImageSpec.ImageDescriptor.ImageOrigin = TgaImageOrigin.BottomLeft;
        tga.ImageArea.ImageData = new byte[]
        {
            0, 0, 255, 255, 0, 0, 255, 255,
            255, 0, 0, 255, 255, 0, 0, 255,
        };
        tga.ToNewFormat(false);
        return tga.ToBytes();
    }

    private static InjectionRecord Record(string id, string name) =>
        new(id, new DateTimeOffset(2026, 9, 15, 14, 41, 0, TimeSpan.FromHours(-4)), SourceConsole.Wii, new TitleId(TitleType.Game, 0x10101D00), @"C:\roms\game.iso", name,
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "WXYZ")));

    private static string Write(string folder, string name, byte value)
    {
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, new[] { value });
        return path;
    }
}
