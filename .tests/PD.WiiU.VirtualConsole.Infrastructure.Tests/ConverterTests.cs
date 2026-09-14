using NAudio.Wave;
using PD.WiiU.VirtualConsole.Infrastructure;
using SkiaSharp;
using TargaSharp;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class ConverterTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public async Task NAudioBootSoundConverter_Wav_WritesBtsndForTarget()
    {
        var wav = Path.Combine(_root, "boot.wav");
        var btsnd = Path.Combine(_root, "bootSound.btsnd");
        using (var writer = new WaveFileWriter(wav, new WaveFormat(48000, 16, 2)))
            writer.WriteSamples(new short[48000 * 2], 0, 48000 * 2);

        await new NAudioBootSoundConverter(BootSoundTarget.Tv).ConvertAsync(wav, btsnd);

        var sound = BootSound.Load(btsnd);
        Assert.AreEqual(BootSoundTarget.Tv, sound.Target);
        Assert.AreEqual(48000, sound.FrameCount);
    }

    [TestMethod]
    public async Task NAudioBootSoundConverter_Cancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => new NAudioBootSoundConverter().ConvertAsync("a.wav", "b.btsnd", cts.Token));
    }

    [TestMethod]
    public async Task SkiaImageConverter_Png_WritesTgaMatchingSlot()
    {
        var png = Path.Combine(_root, "icon.png");
        var tga = Path.Combine(_root, "iconTex.tga");
        using (var bitmap = new SKBitmap(64, 32))
        {
            bitmap.Erase(SKColors.Orange);
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(png, data.ToArray());
        }

        await new SkiaImageConverter().ConvertAsync(png, ImageSlot.Icon, tga);

        var file = new TgaFile(tga);
        Assert.AreEqual(128, file.Width);
        Assert.AreEqual(128, file.Height);
        Assert.AreEqual(TgaPixelDepth.Bpp32, file.Header.ImageSpec.PixelDepth);
    }

    [TestMethod]
    public async Task SkiaImageConverter_Cancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => new SkiaImageConverter().ConvertAsync("a.png", ImageSlot.Icon, "b.tga", cts.Token));
    }
}
