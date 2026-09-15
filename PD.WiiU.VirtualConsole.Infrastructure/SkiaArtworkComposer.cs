using System.Reflection;
using PD.WiiU.VirtualConsole.Ports;
using SkiaSharp;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Draws icons, boot screens and boot logos with SkiaSharp: screenshot into the frame's window, frame on top, then the text.
/// </summary>
public sealed class SkiaArtworkComposer : IArtworkComposer
{
    /// <summary>
    /// Embedded resource prefix the bundled frames live under.
    /// </summary>
    public const string ResourcePrefix = "PD.WiiU.VirtualConsole.Infrastructure.Assets.Frames.";

    private const float DetailSize = 25;
    private const float LogoSize = 20;
    private const float NameSize = 37;

    private static readonly SKColor DarkGround = new(30, 30, 30);
    private static readonly SKColor Fill = new(32, 32, 32);
    private static readonly SKColor IconText = new(147, 149, 152);
    private static readonly SKColor LogoInk = new(180, 180, 180);
    private static readonly SKColor Outline = new(222, 222, 222);
    private static readonly SKColor Shadow = new(190, 190, 190);

    private readonly Func<string?> _captionFontPath;
    private readonly string? _frameDirectory;
    private SKTypeface? _captionTypeface;
    private string? _captionTypefacePath;

    /// <summary>
    /// Creates a new instance of the <see cref="SkiaArtworkComposer"/> class.
    /// </summary>
    /// <param name="frameDirectory">Folder holding frame files; null to use the embedded ones.</param>
    /// <param name="captionFontPath">Resolves the caption font file each time one is drawn; null or a missing file falls back to the bundled UI font.</param>
    public SkiaArtworkComposer(string? frameDirectory = null, Func<string?>? captionFontPath = null)
    {
        _frameDirectory = frameDirectory;
        _captionFontPath = captionFontPath ?? (() => null);
    }

    /// <summary>
    /// Family name of the font captions are drawn in right now.
    /// </summary>
    public string CaptionFontFamily => CaptionTypeface()?.FamilyName ?? FallbackTypeface(SKFontStyle.Bold).FamilyName;

    /// <inheritdoc/>
    public Task ComposeAsync(ArtworkRequest request, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (slot is null)
            throw new ArgumentNullException(nameof(slot));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path is required.", nameof(destinationPath));
        if (request.Frame is { } frame && frame.Slot != slot && !(frame.Slot == ImageSlot.BootTv && slot == ImageSlot.BootDrc))
            throw new ArgumentException($"{frame.Name} is a {frame.Slot.Name} frame, not {slot.Name}.", nameof(request));

        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = slot == ImageSlot.Icon ? DrawIcon(request)
            : slot == ImageSlot.BootLogo ? DrawLogo(request)
            : DrawBootScreen(request, slot);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        using var output = File.Create(destinationPath);
        bitmap.Encode(output, SKEncodedImageFormat.Png, 100);
        return Task.CompletedTask;
    }

    /// <summary>
    /// The 128x128 menu icon: dark ground, screenshot, frame; the plain one is captioned "Virtual Console".
    /// </summary>
    /// <param name="request">What to draw.</param>
    private SKBitmap DrawIcon(ArtworkRequest request)
    {
        var bitmap = new SKBitmap(ImageSlot.Icon.Width, ImageSlot.Icon.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(DarkGround);
        DrawScreenshot(canvas, request.ScreenshotPath, Window(request, ImageSlot.Icon));

        using var frame = LoadFrame(request.Frame);
        if (frame is not null)
        {
            canvas.DrawImage(frame, new SKRect(0, 0, ImageSlot.Icon.Width, ImageSlot.Icon.Height));
            return bitmap;
        }

        var text = "Virtual Console";
        using var font = Font(text, 9.2f, bold: true);
        using var paint = new SKPaint { Color = IconText, IsAntialias = true };
        canvas.DrawText(text, (ImageSlot.Icon.Width - font.MeasureText(text)) / 2, 119, font, paint);
        return bitmap;
    }

    /// <summary>
    /// The 170x42 boot logo: dark ground, frame, then the text centred in the pill.
    /// </summary>
    /// <param name="request">What to draw.</param>
    private SKBitmap DrawLogo(ArtworkRequest request)
    {
        var bitmap = new SKBitmap(ImageSlot.BootLogo.Width, ImageSlot.BootLogo.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(DarkGround);
        using var frame = LoadFrame(request.Frame);
        if (frame is not null)
            canvas.DrawImage(frame, new SKRect(0, 0, ImageSlot.BootLogo.Width, ImageSlot.BootLogo.Height));

        if (string.IsNullOrWhiteSpace(request.LogoText))
            return bitmap;

        var text = request.LogoText!.Trim();
        var box = ArtworkFrames.LogoText;
        using var font = Font(text, LogoSize, bold: true);
        while (font.Size > 5 && font.MeasureText(text) > box.Width - 2)
            font.Size -= 1;

        using var ink = new SKPaint { Color = LogoInk, IsAntialias = true };
        var x = box.X + (box.Width - font.MeasureText(text)) / 2;
        var y = box.Y + (box.Height - font.Metrics.Descent - font.Metrics.Ascent) / 2;
        canvas.DrawText(text, x, y, font, ink);
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
            DrawScreenshot(canvas, request.ScreenshotPath, Window(request, ImageSlot.BootTv));
            using var frame = LoadFrame(request.Frame);
            if (frame is not null)
                canvas.DrawImage(frame, new SKRect(0, 0, ImageSlot.BootTv.Width, ImageSlot.BootTv.Height));

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
    private void DrawCaption(SKCanvas canvas, string text, float size, bool bold, float x, float y, float shadowWidth, float outlineWidth)
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
    /// Stretches the screenshot into its window, or fills it black when there is none; no window draws nothing.
    /// </summary>
    /// <param name="canvas">Canvas to draw on.</param>
    /// <param name="path">Screenshot path, or null.</param>
    /// <param name="area">Where it goes, or null.</param>
    private static void DrawScreenshot(SKCanvas canvas, string? path, PixelRect? area)
    {
        if (area is not { } window)
            return;

        var target = new SKRect(window.X, window.Y, window.Right, window.Bottom);
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
    /// The screenshot window: the frame's, else the slot's plain one.
    /// </summary>
    /// <param name="request">What is being drawn.</param>
    /// <param name="slot">Slot being drawn.</param>
    private static PixelRect? Window(ArtworkRequest request, ImageSlot slot) => request.Frame?.Window ?? ArtworkFrames.DefaultWindow(slot);

    /// <summary>
    /// A font that can draw the text at the size the stock screens use; falls back to whatever the system has for scripts the first face lacks.
    /// </summary>
    /// <param name="text">Text to be drawn.</param>
    /// <param name="size">Point size.</param>
    /// <param name="bold">True for the heavier weight.</param>
    private SKFont Font(string text, float size, bool bold)
    {
        var style = bold ? SKFontStyle.Bold : SKFontStyle.Normal;
        var typeface = CaptionTypeface() ?? FallbackTypeface(style);
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
    /// The chosen caption typeface, reloaded when the path changes; null when there is none or it cannot be read.
    /// </summary>
    private SKTypeface? CaptionTypeface()
    {
        var path = _captionFontPath();
        if (string.Equals(path, _captionTypefacePath, StringComparison.OrdinalIgnoreCase))
            return _captionTypeface;

        _captionTypeface?.Dispose();
        _captionTypefacePath = path;
        _captionTypeface = path is not null && File.Exists(path) ? SKTypeface.FromFile(path) : null;
        return _captionTypeface;
    }

    /// <summary>
    /// The bundled UI font, or the closest system face.
    /// </summary>
    /// <param name="style">Weight wanted.</param>
    private static SKTypeface FallbackTypeface(SKFontStyle style) =>
        SKTypeface.FromFamilyName("Nunito", style) ?? SKTypeface.FromFamilyName("Trebuchet MS", style) ?? SKTypeface.Default;

    /// <summary>
    /// Reads a frame's art from the folder given at construction, else from the embedded copies; null when there is none.
    /// </summary>
    /// <param name="frame">Frame to load, or null.</param>
    private SKImage? LoadFrame(ArtworkFrame? frame)
    {
        if (frame?.Resource is not { } name)
            return null;

        SKBitmap? bitmap;
        if (_frameDirectory is not null)
        {
            var path = Path.Combine(_frameDirectory, name);
            bitmap = File.Exists(path) ? SKBitmap.Decode(path) : null;
        }
        else
        {
            using var stream = typeof(SkiaArtworkComposer).GetTypeInfo().Assembly.GetManifestResourceStream(ResourcePrefix + name);
            bitmap = stream is null ? null : SKBitmap.Decode(stream);
        }

        if (bitmap is null)
            return null;

        using (bitmap)
            return SKImage.FromBitmap(bitmap);
    }
}
