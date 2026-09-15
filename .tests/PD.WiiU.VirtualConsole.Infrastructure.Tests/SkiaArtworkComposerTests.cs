using SkiaSharp;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class SkiaArtworkComposerTests
{
    private string _root = null!;
    private string _screenshot = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "artwork-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _screenshot = Write("shot.png", 64, 64, SKColors.Red);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public async Task ComposeAsync_PlainIcon_PutsTheScreenshotInItsWindowOnTheDarkGround()
    {
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "icon.png");

        await composer.ComposeAsync(new ArtworkRequest(null) { ScreenshotPath = _screenshot }, ImageSlot.Icon, output);

        using var icon = SKBitmap.Decode(output);
        Assert.AreEqual(ImageSlot.Icon.Width, icon.Width);
        Assert.AreEqual(ImageSlot.Icon.Height, icon.Height);
        Assert.AreEqual(SKColors.Red, icon.GetPixel(64, 50), "the screenshot fills its window");
        Assert.AreEqual(new SKColor(30, 30, 30), icon.GetPixel(1, 1), "the ground shows around it");
    }

    [TestMethod]
    public async Task ComposeAsync_IconWithAFrame_DrawsTheFrameOverTheScreenshot()
    {
        var art = Write("IconFrame.png", 128, 128, SKColors.Blue);
        var frame = new ArtworkFrame("t", "Test", ImageSlot.Icon, null, Path.GetFileName(art), ArtworkFrames.IconBadged);
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "icon.png");

        await composer.ComposeAsync(new ArtworkRequest(frame) { ScreenshotPath = _screenshot }, ImageSlot.Icon, output);

        using var icon = SKBitmap.Decode(output);
        Assert.AreEqual(SKColors.Blue, icon.GetPixel(64, 50));
    }

    [TestMethod]
    public async Task ComposeAsync_NoScreenshot_LeavesTheWindowBlack()
    {
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "icon.png");

        await composer.ComposeAsync(new ArtworkRequest(null), ImageSlot.Icon, output);

        using var icon = SKBitmap.Decode(output);
        Assert.AreEqual(SKColors.Black, icon.GetPixel(64, 50));
    }

    [TestMethod]
    public async Task ComposeAsync_BootScreen_IsTvSizedWithTheScreenshotAndCaptions()
    {
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "boot.png");
        var request = new ArtworkRequest(null) { ScreenshotPath = _screenshot, NameLine1 = "Test", ReleaseYear = 1985, Players = 2 };

        await composer.ComposeAsync(request, ImageSlot.BootTv, output);

        using var boot = SKBitmap.Decode(output);
        Assert.AreEqual(ImageSlot.BootTv.Width, boot.Width);
        Assert.AreEqual(ImageSlot.BootTv.Height, boot.Height);
        Assert.AreEqual(SKColors.Red, boot.GetPixel(300, 400), "the screenshot fills its window");
        Assert.AreEqual(SKColors.White, boot.GetPixel(10, 10), "the ground is white");
        Assert.IsTrue(HasInk(boot, 578, 340, 400, 60), "the name is drawn");
        Assert.IsTrue(HasInk(boot, 586, 450, 400, 45), "the release year is drawn");
        Assert.IsTrue(HasInk(boot, 586, 496, 400, 45), "the player count is drawn");
    }

    [TestMethod]
    public async Task ComposeAsync_GamePadBootScreen_TakesATvFrameAtGamePadSize()
    {
        var art = Write("Tv.png", 1280, 720, new SKColor(0, 255, 0, 40));
        var frame = new ArtworkFrame("t", "Test", ImageSlot.BootTv, null, Path.GetFileName(art), ArtworkFrames.BootGba);
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "drc.png");

        await composer.ComposeAsync(new ArtworkRequest(frame) { ScreenshotPath = _screenshot }, ImageSlot.BootDrc, output);

        using var drc = SKBitmap.Decode(output);
        Assert.AreEqual(ImageSlot.BootDrc.Width, drc.Width);
        Assert.AreEqual(ImageSlot.BootDrc.Height, drc.Height);
        var inWindow = drc.GetPixel(200, 266);
        Assert.IsTrue(inWindow.Red > 200 && inWindow.Green > 20, "screenshot tinted by the frame");
    }

    [TestMethod]
    public async Task ComposeAsync_BootLogo_IsLogoSizedWithCentredText()
    {
        var art = Write("Pill.png", 170, 42, new SKColor(255, 255, 255, 0));
        var frame = new ArtworkFrame("t", "Pill", ImageSlot.BootLogo, null, Path.GetFileName(art), null);
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "logo.png");

        await composer.ComposeAsync(new ArtworkRequest(frame) { LogoText = "Super Metroid" }, ImageSlot.BootLogo, output);

        using var logo = SKBitmap.Decode(output);
        Assert.AreEqual(ImageSlot.BootLogo.Width, logo.Width);
        Assert.AreEqual(ImageSlot.BootLogo.Height, logo.Height);
        Assert.AreEqual(new SKColor(30, 30, 30), logo.GetPixel(2, 2), "dark ground");
        Assert.IsTrue(HasInkOn(logo, new SKColor(30, 30, 30), 18, 5, 134, 32), "text drawn inside the pill");
    }

    [TestMethod]
    public async Task ComposeAsync_LogoWithLongText_ShrinksToFit()
    {
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "logo.png");

        await composer.ComposeAsync(new ArtworkRequest(null) { LogoText = "An Unreasonably Long Game Title Here" }, ImageSlot.BootLogo, output);

        using var logo = SKBitmap.Decode(output);
        Assert.IsFalse(HasInkOn(logo, new SKColor(30, 30, 30), 0, 0, 16, 42), "nothing spills past the left of the text box");
        Assert.IsFalse(HasInkOn(logo, new SKColor(30, 30, 30), 154, 0, 16, 42), "nothing spills past the right of the text box");
    }

    [TestMethod]
    public async Task ComposeAsync_FrameForAnotherSlot_Throws()
    {
        var composer = new SkiaArtworkComposer(_root);
        var icon = new ArtworkFrame("i", "Icon", ImageSlot.Icon, null, null, ArtworkFrames.IconStandard);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => composer.ComposeAsync(new ArtworkRequest(icon), ImageSlot.BootTv, Path.Combine(_root, "x.png")));
    }

    [TestMethod]
    public async Task ComposeAsync_Nulls_Throw()
    {
        var composer = new SkiaArtworkComposer(_root);
        var request = new ArtworkRequest(null);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => composer.ComposeAsync(null!, ImageSlot.Icon, Path.Combine(_root, "x.png")));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => composer.ComposeAsync(request, null!, Path.Combine(_root, "x.png")));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => composer.ComposeAsync(request, ImageSlot.Icon, " "));
    }

    [TestMethod]
    public async Task ComposeAsync_EveryBundledFrame_HasItsArtEmbedded()
    {
        var composer = new SkiaArtworkComposer();
        foreach (var frame in ArtworkFrames.All.Where(f => !f.IsPlain))
        {
            var output = Path.Combine(_root, frame.Key + ".png");
            await composer.ComposeAsync(new ArtworkRequest(frame) { ScreenshotPath = _screenshot, LogoText = "x" }, frame.Slot, output);

            using var image = SKBitmap.Decode(output);
            Assert.IsFalse(IsBlank(image), frame.Key + " drew nothing");
        }
    }

    [TestMethod]
    public void CaptionFontFamily_FontFileGiven_UsesItAndFollowsChanges()
    {
        var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UWUVCI AIO", "bin", "Tools", CaptionFont.LegacyFileName);
        if (!File.Exists(legacy))
            Assert.Inconclusive("the previous application's font is not on this machine");

        string? path = null;
        var composer = new SkiaArtworkComposer(_root, () => path);
        var fallback = composer.CaptionFontFamily;

        path = legacy;
        StringAssert.Contains(composer.CaptionFontFamily, "Rodin");

        path = Path.Combine(_root, "missing.otf");
        Assert.AreEqual(fallback, composer.CaptionFontFamily, "a missing file falls back");
    }

    private static bool HasInk(SKBitmap bitmap, int x, int y, int width, int height) => HasInkOn(bitmap, SKColors.White, x, y, width, height);

    private static bool HasInkOn(SKBitmap bitmap, SKColor ground, int x, int y, int width, int height)
    {
        for (var row = y; row < y + height; row++)
            for (var column = x; column < x + width; column++)
                if (bitmap.GetPixel(column, row) != ground)
                    return true;

        return false;
    }

    private static bool IsBlank(SKBitmap bitmap)
    {
        var first = bitmap.GetPixel(0, 0);
        for (var y = 0; y < bitmap.Height; y += 2)
            for (var x = 0; x < bitmap.Width; x += 2)
                if (bitmap.GetPixel(x, y) != first)
                    return false;

        return true;
    }

    private string Write(string name, int width, int height, SKColor colour)
    {
        var path = Path.Combine(_root, name);
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(colour);

        using var stream = File.Create(path);
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        return path;
    }
}
