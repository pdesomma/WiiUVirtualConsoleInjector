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
    public async Task ComposeAsync_IconWithoutAFrame_PutsTheScreenshotInItsWindowOnTheDarkGround()
    {
        var composer = new SkiaArtworkComposer(_root);
        var request = new ArtworkRequest(Template(null, null)) { ScreenshotPath = _screenshot };
        var output = Path.Combine(_root, "icon.png");

        await composer.ComposeAsync(request, ImageSlot.Icon, output);

        using var icon = SKBitmap.Decode(output);
        Assert.AreEqual(ImageSlot.Icon.Width, icon.Width);
        Assert.AreEqual(ImageSlot.Icon.Height, icon.Height);
        Assert.AreEqual(SKColors.Red, icon.GetPixel(64, 50), "the screenshot fills its window");
        Assert.AreEqual(new SKColor(30, 30, 30), icon.GetPixel(1, 1), "the ground shows around it");
    }

    [TestMethod]
    public async Task ComposeAsync_IconWithAFrame_DrawsTheFrameOverTheScreenshot()
    {
        var frame = Write("IconFrame.png", 128, 128, new SKColor(0, 0, 255, 255));
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "icon.png");

        await composer.ComposeAsync(new ArtworkRequest(Template(null, Path.GetFileName(frame))) { ScreenshotPath = _screenshot }, ImageSlot.Icon, output);

        using var icon = SKBitmap.Decode(output);
        Assert.AreEqual(SKColors.Blue, icon.GetPixel(64, 50));
    }

    [TestMethod]
    public async Task ComposeAsync_NoScreenshot_LeavesTheWindowBlack()
    {
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "icon.png");

        await composer.ComposeAsync(new ArtworkRequest(Template(null, null)), ImageSlot.Icon, output);

        using var icon = SKBitmap.Decode(output);
        Assert.AreEqual(SKColors.Black, icon.GetPixel(64, 50));
    }

    [TestMethod]
    public async Task ComposeAsync_BootScreen_IsTvSizedWithTheScreenshotAndCaptions()
    {
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "boot.png");
        var request = new ArtworkRequest(Template(null, null))
        {
            ScreenshotPath = _screenshot,
            NameLine1 = "Test",
            ReleaseYear = 1985,
            Players = 2,
        };

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
    public async Task ComposeAsync_GamePadBootScreen_IsTheSameImageAtGamePadSize()
    {
        var composer = new SkiaArtworkComposer(_root);
        var output = Path.Combine(_root, "drc.png");

        await composer.ComposeAsync(new ArtworkRequest(Template(null, null)) { ScreenshotPath = _screenshot }, ImageSlot.BootDrc, output);

        using var drc = SKBitmap.Decode(output);
        Assert.AreEqual(ImageSlot.BootDrc.Width, drc.Width);
        Assert.AreEqual(ImageSlot.BootDrc.Height, drc.Height);
        Assert.AreEqual(SKColors.Red, drc.GetPixel(200, 266));
    }

    [TestMethod]
    public async Task ComposeAsync_BootLogoSlotOrNulls_Throw()
    {
        var composer = new SkiaArtworkComposer(_root);
        var request = new ArtworkRequest(Template(null, null));

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => composer.ComposeAsync(request, ImageSlot.BootLogo, Path.Combine(_root, "x.png")));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => composer.ComposeAsync(null!, ImageSlot.Icon, Path.Combine(_root, "x.png")));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => composer.ComposeAsync(request, null!, Path.Combine(_root, "x.png")));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => composer.ComposeAsync(request, ImageSlot.Icon, " "));
    }

    [TestMethod]
    public async Task ComposeAsync_EveryBundledTemplate_HasItsFramesEmbedded()
    {
        var composer = new SkiaArtworkComposer();
        foreach (var template in ArtworkTemplates.All)
        {
            var output = Path.Combine(_root, template.Key + ".png");
            await composer.ComposeAsync(new ArtworkRequest(template) { ScreenshotPath = _screenshot }, ImageSlot.Icon, output);

            using var icon = SKBitmap.Decode(output);
            Assert.IsFalse(IsBlank(icon), template.Key + " drew nothing");
        }
    }

    private static bool HasInk(SKBitmap bitmap, int x, int y, int width, int height)
    {
        for (var row = y; row < y + height; row++)
            for (var column = x; column < x + width; column++)
                if (bitmap.GetPixel(column, row) != SKColors.White)
                    return true;

        return false;
    }

    private static bool IsBlank(SKBitmap bitmap)
    {
        var first = bitmap.GetPixel(0, 0);
        for (var y = 0; y < bitmap.Height; y += 4)
            for (var x = 0; x < bitmap.Width; x += 4)
                if (bitmap.GetPixel(x, y) != first)
                    return false;

        return true;
    }

    private static ArtworkTemplate Template(string? bootFrame, string? iconFrame) =>
        new("test", "Test", SourceConsole.Nes, ArtworkLayout.Standard, bootFrame, iconFrame);

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
