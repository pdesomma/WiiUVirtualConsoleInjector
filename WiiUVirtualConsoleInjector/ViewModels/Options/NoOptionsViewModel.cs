using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Options panel for consoles that have none.
/// </summary>
public sealed class NoOptionsViewModel : ConsoleOptionsViewModel
{
    /// <summary>
    /// Creates a new instance of the <see cref="NoOptionsViewModel"/> class.
    /// </summary>
    /// <param name="console">Console without options.</param>
    public NoOptionsViewModel(SourceConsole console)
        : base(console)
    {
    }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => null;
}
