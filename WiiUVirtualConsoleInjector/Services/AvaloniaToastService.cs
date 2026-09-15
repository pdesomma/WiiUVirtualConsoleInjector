using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Toasts through a <see cref="WindowNotificationManager"/> on the owning window.
/// </summary>
public sealed class AvaloniaToastService : IToastService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(2);

    private readonly Func<Window> _owner;
    private WindowNotificationManager? _manager;

    /// <summary>
    /// Creates a new instance of the <see cref="AvaloniaToastService"/> class.
    /// </summary>
    /// <param name="owner">Resolves the window the toasts appear in.</param>
    public AvaloniaToastService(Func<Window> owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc/>
    public void Show(string message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        _manager ??= new WindowNotificationManager(_owner()) { Position = NotificationPosition.BottomRight, MaxItems = 3 };
        _manager.Show(new Notification(message, null, NotificationType.Success, Lifetime));
    }
}
