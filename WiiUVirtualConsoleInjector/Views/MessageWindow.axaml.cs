using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WiiUVirtualConsoleInjector.Views;

/// <summary>
/// A modal notice, error or yes/no question.
/// </summary>
public partial class MessageWindow : Window
{
    /// <summary>
    /// Creates a new instance of the <see cref="MessageWindow"/> class.
    /// </summary>
    public MessageWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// What the window asks or tells.
    /// </summary>
    public enum Kind
    {
        Information,
        Error,
        Question,
    }

    /// <summary>
    /// Shows the window modally; true when the user chose yes or dismissed a notice.
    /// </summary>
    /// <param name="owner">Window to be modal to.</param>
    /// <param name="title">Window title.</param>
    /// <param name="message">Body text.</param>
    /// <param name="kind">Buttons to offer.</param>
    public static async Task<bool> ShowAsync(Window owner, string title, string message, Kind kind)
    {
        var window = new MessageWindow { Title = title };
        window.TitleText.Text = title;
        window.MessageText.Text = message;
        window.NoButton.IsVisible = kind == Kind.Question;
        window.YesButton.Content = kind == Kind.Question ? "Continue" : "OK";
        return await window.ShowDialog<bool?>(owner).ConfigureAwait(true) ?? false;
    }

    private void OnNo(object? sender, RoutedEventArgs e) => Close(false);

    private void OnYes(object? sender, RoutedEventArgs e) => Close(true);
}
