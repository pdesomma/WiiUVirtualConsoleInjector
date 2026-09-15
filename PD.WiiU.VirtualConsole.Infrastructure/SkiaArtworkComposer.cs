using System.Reflection;
using PD.WiiU.VirtualConsole.Ports;
using SkiaSharp;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Draws icons and boot screens with SkiaSharp: screenshot into the frame's window, frame on top, then the caption text.
/// </summary>
public sealed class SkiaArtworkComposer : IArtworkComposer
{
    /// <summary>
    /// Folder frames are read from when one is given; otherwise they come from the embedded copies.
    /// </summary>
    public const string ResourcePrefix = "PD.WiiU.VirtualConsole.Infrastructure.Assets.Frames.";

    private const float DetailSize = 25;
    private const float NameSize = 37;

    private static readonly SKColor Fill = new(32, 32, 32);
    private static readonly SKColor IconBackground = new(30, 30, 30);
    private static readonly SKColor IconText = new(147, 149, 152);
    private static readonly SKColor Outline = new(222, 222, 222);
    private static readonly SKColor Shadow = new(190, 190, 190);

    private readonly string? _frameDirectory;

    /// <summary>
    /// Creates a new instance of the <see cref="SkiaArtworkComposer"/> class.
    /// </summary>
    /// <param name="frameDirectory">Folder holding frame files; null to use the embedded ones.</param>
    public SkiaArtworkComposer(string? frameDirectory = null)
    {
        _frameDirectory = frameDirectory;
    }

    /// <inheritdoc/>
    public Task ComposeAsync(ArtworkRequest request, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (slot is null)
            throw new ArgumentNullException(nameof(slot));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path is required.", nameof(destinationPath));
        if (slot != ImageSlot.Icon && slot != ImageSlot.BootTv && slot != ImageSlot.BootDrc)
            throw new NotSupportedException($"{slot.Name} is not drawn from a screenshot.");

        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = slot == ImageSlot.Icon ? DrawIcon(request) : DrawBootScreen(request, slot);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        using var output = File.Create(destinationPath);
        bitmap.Encode(output, SKEncodedImageFormat.Png, 100);
        return Task.CompletedTask;
    }

    /// <summary>
    /// The 128x128 menu icon.
    /// </summary>
    /// <param name="request">What to draw.</param>
    private SKBitmap DrawIcon(ArtworkRequest request)
    {
        var bitmap = new SKBitmap(ImageSlot.Icon.Width, ImageSlot.Icon.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(IconBackground);
        DrawScreenshot(canvas, request.ScreenshotPath, request.Template.Layout.IconArea);

        using var frame = LoadFrame(request.Template.IconFrame);
        if (frame is not null)
        {
            canvas.DrawImage(SKImage.FromBitmap(frame), new SKRect(0, 0, ImageSlot.Icon.Width, ImageSlot.Icon.Height));
            return bitmap;
        }

        var text = "Virtual Console";
        using var font = Font(text, 9.2f, bold: true);
        using var paint = new SKPaint { Color = IconText, IsAntialias = true };
        var width = font.MeasureText(text);
        canvas.DrawText(text, (ImageSlot.Icon.Width - width) / 2, 119, font, paint);
        return bitmap;
    }

    /// <summary>
    /// A boot screen, drawn at 1280x720 and scaled down for the GamePad.
    /// </summary>
    /// <param name="request">What to draw.</param>
    /// <param name="slot">Boot slot being drawn.</param>
    private SKBitmap DrawBootScreen(ArtworkRequest request, ImageSlot slot)
    {
        var bitmap = new SKBitmap(ImageSlot.BootTv.Width, ImageSlot.BootTv.Height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            DrawScreenshot(canvas, request.ScreenshotPath, request.Template.Layout.BootArea);
            using var frame = LoadFrame(request.Template.BootFrame);
            if (frame is not null)
                canvas.DrawImage(SKImage.FromBitmap(frame), new SKRect(0, 0, ImageSlot.BootTv.Width, ImageSlot.BootTv.Height));

            var twoLines = !string.IsNullOrWhiteSpace(request.NameLine2);
            if (!string.IsNullOrWhiteSpace(request.NameLine1))
                DrawCaption(canvas, request.NameLine1!, NameSize, bold: true, 578, twoLines ? 350 : 377, 7, 5);
            if (twoLines)
                DrawCaption(canvas, request.NameLine2!, NameSize, bold: true, 578, 405, 7, 5);

            if (request.ReleasedText is { } released)
                DrawCaption(canvas, released, DetailSize, bold: false, 586, 480, 6, 4);
            if (request.PlayersText is { } players)
                DrawCaption(canvas, players, DetailSize, bold: false, 586, 526, 6, 4);
        }

        if (slot == ImageSlot.BootTv)
            return bitmap;

        var resized = bitmap.Resize(new SKImageInfo(slot.Width, slot.Height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        bitmap.Dispose();
        return resized;
    }

    /// <summary>
    /// One line of caption text: shadow, outline, then fill, the way the stock screens look.
    /// </summary>
    /// <param name="canvas">Canvas to draw on.</param>
    /// <param name="text">Line to draw.</param>
    /// <param name="size">Point size.</param>
    /// <param name="bold">True for the heavier weight.</param>
    /// <param name="x">Left edge.</param>
    /// <param name="y">Text baseline.</param>
    /// <param name="shadowWidth">Width of the outer stroke.</param>
    /// <param name="outlineWidth">Width of the inner stroke.</param>
    private static void DrawCaption(SKCanvas canvas, string text, float size, bool bold, float x, float y, float shadowWidth, float outlineWidth)
    {
        using var font = Font(text, size, bold);
        using var shadow = new SKPaint { Color = Shadow, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = shadowWidth, StrokeJoin = SKStrokeJoin.Round };
        using var outline = new SKPaint { Color = Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = outlineWidth, StrokeJoin = SKStrokeJoin.Round };
        using var fill = new SKPaint { Color = Fill, IsAntialias = true };
        canvas.DrawText(text, x, y, font, shadow);
        canvas.DrawText(text, x, y, font, outline);
        canvas.DrawText(text, x, y, font, fill);
    }

    /// <summary>
    /// Stretches the screenshot into its window, or fills it black when there is none.
    /// </summary>
    /// <param name="canvas">Canvas to draw on.</param>
    /// <param name="path">Screenshot path, or null.</param>
    /// <param name="area">Where it goes.</param>
    private static void DrawScreenshot(SKCanvas canvas, string? path, PixelRect area)
    {
        var target = new SKRect(area.X, area.Y, area.Right, area.Bottom);
        using var screenshot = string.IsNullOrWhiteSpace(path) ? null : SKBitmap.Decode(path);
        if (screenshot is null)
        {
            using var black = new SKPaint { Color = SKColors.Black };
            canvas.DrawRect(target, black);
            return;
        }

        using var image = SKImage.FromBitmap(screenshot);
        canvas.DrawImage(image, target, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
    }

    /// <summary>
    /// A font that can draw the text at the size the stock screens use; falls back to whatever the system has for scripts the first face lacks.
    /// </summary>
    /// <param name="text">Text to be drawn.</param>
    /// <param name="size">Point size.</param>
    /// <param name="bold">True for the heavier weight.</param>
    private static SKFont Font(string text, float size, bool bold)
    {
        var style = bold ? SKFontStyle.Bold : SKFontStyle.Normal;
        var typeface = SKTypeface.FromFamilyName("Nunito", style) ?? SKTypeface.FromFamilyName("Trebuchet MS", style) ?? SKTypeface.Default;
        var font = new SKFont(typeface, size) { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias };
        var missing = text.FirstOrDefault(c => !char.IsWhiteSpace(c) && !font.ContainsGlyph(c));
        if (missing == default)
            return font;

        var fallback = SKFontManager.Default.MatchCharacter(null, style, null, missing);
        if (fallback is null)
            return font;

        font.Dispose();
        return new SKFont(fallback, size) { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias };
    }

    /// <summary>
    /// Reads a frame from the folder given at construction, else from the embedded copies; null when there is no frame.
    /// </summary>
    /// <param name="name">Frame file name, or null.</param>
    private SKBitmap? LoadFrame(string? name)
    {
        if (name is null)
            return null;
        if (_frameDirectory is not null)
        {
            var path = Path.Combine(_frameDirectory, name);
            return File.Exists(path) ? SKBitmap.Decode(path) : null;
        }

        using var stream = typeof(SkiaArtworkComposer).GetTypeInfo().Assembly.GetManifestResourceStream(ResourcePrefix + name);
        return stream is null ? null : SKBitmap.Decode(stream);
    }
}
