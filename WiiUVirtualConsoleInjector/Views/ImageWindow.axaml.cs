using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace WiiUVirtualConsoleInjector.Views;

/// <summary>
/// A picture at its own pixels, shrunk only when the screen is smaller.
/// </summary>
public partial class ImageWindow : Window
{
    /// <summary>
    /// Creates a new instance of the <see cref="ImageWindow"/> class.
    /// </summary>
    public ImageWindow()
    {
        InitializeComponent();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                Close();
        };
    }

    /// <summary>
    /// Opens the image over the owner and returns when it is closed; a file that cannot be decoded is shown as a note instead.
    /// </summary>
    /// <param name="owner">Window to centre on.</param>
    /// <param name="title">Caption.</param>
    /// <param name="path">Image file.</param>
    public static Task ShowAsync(Window owner, string title, string path)
    {
        if (owner is null)
            throw new ArgumentNullException(nameof(owner));

        var bitmap = Converters.FileImageConverter.Load(path);
        if (bitmap is null)
            return MessageWindow.ShowAsync(owner, title, "This file cannot be shown as a picture.", MessageWindow.Kind.Information);

        var window = new ImageWindow { Title = title };
        window.TitleText.Text = title;
        window.SizeText.Text = $"{bitmap.PixelSize.Width} x {bitmap.PixelSize.Height}";
        window.Picture.Source = bitmap;
        window.Fit(bitmap, owner);
        return window.ShowDialog(owner);
    }

    /// <summary>
    /// Sizes the picture 1:1 up to what fits on the owner's screen.
    /// </summary>
    /// <param name="bitmap">The picture.</param>
    /// <param name="owner">Whose screen bounds it.</param>
    private void Fit(Bitmap bitmap, Window owner)
    {
        var screen = owner.Screens.ScreenFromWindow(owner) ?? owner.Screens.Primary;
        var scale = screen?.Scaling ?? 1;
        // chrome: margins, padding and the two text rows
        var maxWidth = (screen?.WorkingArea.Width ?? 1920) / scale - 120;
        var maxHeight = (screen?.WorkingArea.Height ?? 1080) / scale - 160;
        var width = Math.Min(bitmap.PixelSize.Width, maxWidth);
        var height = Math.Min(bitmap.PixelSize.Height, maxHeight);
        var ratio = Math.Min(width / bitmap.PixelSize.Width, height / bitmap.PixelSize.Height);
        Picture.Width = bitmap.PixelSize.Width * ratio;
        Picture.Height = bitmap.PixelSize.Height * ratio;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e) => Close();
}
